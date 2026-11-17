using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Kawaii Cascades's songs by artist (generated from scenes.json by build/song_ids.py; part of KawaiiKit), so
    /// every one of them can have its own light (KawaiiKit.Song, KawaiiKit.SongLamps).
    public partial class KawaiiKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Alesda!", new[] { ("Nikoniko", "48oqvRrF8xiUFCWdVC3GQZ") } },
            { "Amidst", new[] { ("Glass Petals", "3gaP7H0VvLpuTTFypFG4l8") } },
            { "cYsmix", new[] { ("Escapism - Original Mix", "1kkXrOWL2Ui4xPpusTNTgZ") } },
            { "Diveo", new[] { ("Summer Trees", "0kCsANasMTT4VF3SX0XtUN") } },
            { "DraGonis", new[] { ("Escape Velocity", "4lCpxRcplu4J2OYOpQiBTI"), ("Waiting", "44Zi3Eqx9PbkhNXa37ggGb") } },
            { "Feryquitous", new[] { ("Monochrome Anomaly (feat. F9)", "1SAJNDJO9s3oEi10CItFsU") } },
            { "Flay!", new[] { ("Forgotten", "0NHW63JmrZ4t8bBKjXVclS"), ("In The Sky", "3RA8xOfwcqgnHdr93SB5se"), ("dreamseeker", "1JjhlO7Tu6uYmoHPM7XGaO") } },
            { "Hyleo", new[] { ("Trigger", "0Tdx0c0zBic7hUIgvr6Fkl") } },
            { "kamome sano", new[] { ("infomorph", "3381QXrAF98Fdoo6uXNjTz"), ("sweet syrup", "0b57DGq2YppuDYp5VElkvL") } },
            { "Kizuna AI", new[] { ("future base", "4NtdvYIPuCu7rGVryVgTWG") } },
            { "Mameyudoufu", new[] { ("Moonheart - Mameyudoufu Remix", "3eFJrlCg5ICxPXkCKP4A6d"), ("Quality Control", "748dpc3kw8TvYp9EIRC2x4") } },
            { "Mitsukiyo", new[] { ("Irodori Canvas - Korean Version", "2Z6DzhXc6KqnaqeHwU6Y95") } },
            { "Moon Jelly", new[] { ("Lost", "72kTwzGgET0sMomVTsk6xm") } },
            { "MOTTO MUSIC", new[] { ("Signs of Rain", "6dEcEu4RPwmgH65jPRgITU") } },
            { "Nokae", new[] { ("Swords Dance", "5ow8j2ZFWZCXvKs5P7Q818"), ("Verity", "2n6JqsT8YgR00u91XTzpnP") } },
            { "OISHII", new[] { ("Onigiri Freeway", "0M8ZkEibtau2jSd3qxVlaK") } },
            { "Project Skylate", new[] { ("Ryokucha", "3pQF70y7sZs45S6y9Mdygc") } },
            { "PSYQUI", new[] { ("Endless [feat. Mameyudoufu] - Mameyudoufu Remix", "3PIk9eVJR8bQJt2zSZJBAh") } },
            { "Pure 100%", new[] { ("BREATH", "4HsFiZr0inQmoz9rReElFV") } },
            { "Snail's House", new[] { ("Snailchan Adventure", "5IdRCNAdlvR7bSoXkmB12m") } },
            { "t+pazolite", new[] { ("Do you wanna know?", "6ePCNQ7fQQ4PyuPOB4iykZ") } },
            { "Tanchiky", new[] { ("Sanshisuimei - Original Mix", "0doMlLbXXHbVEi9mkVpiDM") } },
            { "technoplanet", new[] { ("Megastructure", "47JrhvvH4AFUJIk857xwqj") } },
            { "tonytonychopper999", new[] { ("ENERGIZE", "3daBuSSW5j6I2F4cPnayAx") } },
        };
    }
}
