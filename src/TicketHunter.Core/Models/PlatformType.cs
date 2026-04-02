namespace TicketHunter.Core.Models;

public enum PlatformType
{
    Unknown,

    // --- 台灣平台 ---
    Tixcraft,           // tixcraft.com
    Ticketmaster,       // ticketmaster.com / ticketmaster.sg
    Kktix,              // kktix.cc
    IBon,               // ticket.ibon.com.tw / tour.ibon.com.tw
    TicketPlus,         // ticketplus.com.tw (遠大)
    Kham,               // kham.com.tw (寬宏)
    NianDai,            // ticket.com.tw (年代)
    Udn,                // tickets.udnfunlife.com (聯合)
    FamiTicket,         // famiticket.com.tw
    FunOne,             // tickets.funone.io
    FansiGo,            // go.fansi.me
    Indievox,           // indievox.com (TixCraft 家族)

    // --- 海外平台 ---
    Cityline,           // cityline.com (香港)
    Urbtix,             // ticket.urbtix.hk (香港)
    HkTicketing,        // hotshow.hkticketing.com (香港)
    GalaxyMacau,        // galaxymacau.com (澳門)
    TicketekAu          // ticketek.com.au (澳洲)
}
