using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace TicketHunter.Core.Services;

public class OcrService : IDisposable
{
    private readonly ILogger<OcrService> _logger;
    private InferenceSession? _session;
    private string _modelPath;
    private string _charsetPath;
    private bool _initialized;

    /// <summary>
    /// Character set loaded from charset JSON file.
    /// Index 0 = blank (CTC), index 1+ = actual characters.
    /// Supports two formats:
    ///   1. Dict format: {"charset": [" ", "0", ...], "image": [200, 64], "channel": 1}
    ///   2. List format: [" ", "0", ...] (legacy ddddocr)
    /// </summary>
    private List<string> _charset = new();

    /// <summary>Target image dimensions from charset config, defaults to [0, 64] (0 = auto width).</summary>
    private int _targetWidth;
    private int _targetHeight = 64;
    private int _channel = 1;

    public OcrService(ILogger<OcrService> logger, string modelPath = "assets/ocr_models/custom.onnx",
        string charsetPath = "assets/ocr_models/charsets.json")
    {
        _logger = logger;
        _modelPath = modelPath;
        _charsetPath = charsetPath;
    }

    /// <summary>
    /// Override model/charset paths (e.g. from user config). Call before Initialize().
    /// </summary>
    public void SetPaths(string? modelPath, string? charsetPath)
    {
        if (!string.IsNullOrWhiteSpace(modelPath)) _modelPath = modelPath;
        if (!string.IsNullOrWhiteSpace(charsetPath)) _charsetPath = charsetPath;
    }

    public bool Initialize()
    {
        try
        {
            if (!File.Exists(_modelPath))
            {
                _logger.LogWarning("OCR model not found at {Path}", _modelPath);
                return false;
            }

            // Load charset
            if (!File.Exists(_charsetPath))
            {
                _logger.LogWarning("OCR charset not found at {Path}", _charsetPath);
                return false;
            }

            var charsetJson = File.ReadAllText(_charsetPath);

            // Try dict format first: {"charset": [...], "image": [w, h], "channel": 1}
            try
            {
                using var doc = JsonDocument.Parse(charsetJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("charset", out var charsetProp))
                {
                    _charset = charsetProp.Deserialize<List<string>>() ?? new();

                    if (doc.RootElement.TryGetProperty("image", out var imageProp))
                    {
                        var dims = imageProp.Deserialize<List<int>>() ?? new();
                        if (dims.Count >= 2)
                        {
                            _targetWidth = dims[0];
                            _targetHeight = dims[1];
                        }
                    }

                    if (doc.RootElement.TryGetProperty("channel", out var channelProp))
                        _channel = channelProp.GetInt32();
                }
                else
                {
                    // Fallback: plain list format
                    _charset = JsonSerializer.Deserialize<List<string>>(charsetJson) ?? new();
                }
            }
            catch
            {
                _charset = JsonSerializer.Deserialize<List<string>>(charsetJson) ?? new();
            }

            if (_charset.Count == 0)
            {
                _logger.LogWarning("OCR charset is empty");
                return false;
            }

            _logger.LogInformation("OCR charset loaded: {Count} characters, image: {W}x{H}, channel: {Ch}",
                _charset.Count, _targetWidth, _targetHeight, _channel);

            _session = new InferenceSession(_modelPath);
            _initialized = true;
            _logger.LogInformation("OCR model loaded from {Path}", _modelPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load OCR model");
            return false;
        }
    }

    public string? Recognize(byte[] imageBytes)
    {
        if (!_initialized || _session == null)
        {
            _logger.LogWarning("OCR not initialized, attempting initialization");
            if (!Initialize()) return null;
        }

        try
        {
            using var image = Image.Load<Rgba32>(imageBytes);

            // Preprocess: resize using charset config dimensions
            int width, height;
            if (_targetWidth > 0)
            {
                // Fixed dimensions from charset config (e.g. 200x64)
                width = _targetWidth;
                height = _targetHeight;
            }
            else
            {
                // Auto width: keep aspect ratio, scale to target height
                height = _targetHeight;
                width = (int)(image.Width * ((float)height / image.Height));
            }

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(width, height),
                Mode = ResizeMode.Stretch
            }));

            // Create input tensor [1, 1, height, width] (batch, channel=grayscale, h, w)
            var tensor = new DenseTensor<float>(new[] { 1, 1, height, width });

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var pixel = image[x, y];
                    // Convert to grayscale and normalize to [0, 1]
                    var gray = (0.299f * pixel.R + 0.587f * pixel.G + 0.114f * pixel.B) / 255f;
                    tensor[0, 0, y, x] = gray;
                }
            }

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(_session!.InputNames[0], tensor)
            };

            using var results = _session.Run(inputs);
            var output = results.First().AsTensor<float>();

            // Model output shape: [sequence_length, batch=1, num_classes=8210]
            var decoded = CtcGreedyDecode(output);
            _logger.LogInformation("OCR recognized: {Result}", decoded);
            return decoded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR recognition failed");
            return null;
        }
    }

    /// <summary>
    /// CTC greedy decode for ddddocr model output.
    /// Output shape: [sequence_length, 1, num_classes] where index 0 = blank.
    /// </summary>
    private string CtcGreedyDecode(Tensor<float> output)
    {
        var dims = output.Dimensions;
        if (dims.Length != 3) return "";

        int seqLen, numClasses;
        bool seqFirst;

        // Determine layout: [seq, batch, classes] vs [batch, seq, classes]
        if (dims[1] == 1 && dims[0] != 1)
        {
            // [seq, 1, classes] — ddddocr common.onnx format
            seqLen = dims[0];
            numClasses = dims[2];
            seqFirst = true;
        }
        else if (dims[0] == 1)
        {
            // [1, seq, classes]
            seqLen = dims[1];
            numClasses = dims[2];
            seqFirst = false;
        }
        else
        {
            return "";
        }

        var result = new List<string>();
        int prevIdx = -1;

        for (int t = 0; t < seqLen; t++)
        {
            int bestIdx = 0;
            float bestVal = float.MinValue;

            for (int c = 0; c < numClasses; c++)
            {
                float val = seqFirst ? output[t, 0, c] : output[0, t, c];
                if (val > bestVal)
                {
                    bestVal = val;
                    bestIdx = c;
                }
            }

            // CTC: index 0 = blank, skip blanks and consecutive duplicates
            if (bestIdx != 0 && bestIdx != prevIdx)
            {
                if (bestIdx < _charset.Count)
                    result.Add(_charset[bestIdx]);
            }
            prevIdx = bestIdx;
        }

        return string.Join("", result);
    }

    public void Dispose()
    {
        _session?.Dispose();
        GC.SuppressFinalize(this);
    }
}
