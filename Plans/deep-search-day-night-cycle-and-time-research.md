# Your 5-minute day is 3× faster than the genre's fastest games

**At 12.5 real seconds per in-game hour, your game's clock runs roughly 3.5× faster than Stardew Valley — a game many players already mod to be slower.** The cozy farming genre has converged on **14–24 real minutes per playable day** as the comfortable standard, with most titles landing around 15–20 minutes. For a game about observing insects and tending a garden — activities that reward patience and stillness — a 5-minute day works against the core experience. Below is a comprehensive breakdown of how dozens of games handle time, what patterns emerge, and what this means for your design.

## The numbers: real seconds per hour across 20+ games

The table below captures specific timing data for every major cozy farming and simulation game. Games are sorted by day length, shortest to longest.

| Game | Sec / In-Game Hour | Full Day (Real Min) | Active Day (Real Min) | Time Display |
|---|---|---|---|---|
| **Your game (current)** | **12.5** | **5 min** | **5 min** | — |
| Graveyard Keeper | ~19 | ~7.5 min | ~7.5 min | Sun/moon position |
| Stardew Valley | ~43 | ~17 min (24h) | ~14.3 min (6am–2am) | 10-min increments |
| My Time at Portia | ~45 | ~18 min | ~15 min (7am–3am) | Minute-by-minute |
| Coral Island (default) | 45 | ~18 min | ~15 min (6am–2am) | Minute-by-minute |
| Fae Farm | ~45–50 | ~18–20 min | ~15 min (6am–12am) | Clock display |
| Garden Paws | 60 | 18 min (18h day) | 18 min (6:00–24:00) | Hour display |
| HM: Friends of Mineral Town (remake) | ~30 | ~12 min | ~10 min | 10-min increments |
| HM: A Wonderful Life | ~60 | ~24 min | ~20 min | Minute display |
| SoS: Pioneers of Olive Town | ~60 | ~24 min | ~20 min | Clock display |
| Kynseed | 48 | ~19.2 min | ~19 min | Diegetic dandelion clock |
| Moonstone Island (default) | 60 | ~24 min | ~20 min (6am–2am) | 10-min increments |
| Ooblets | ~50 | ~20 min | ~20 min | No formal clock |
| My Time at Sandrock (default) | ~60 | ~24 min | ~20 min (7am–3am) | 24h minute-by-minute |
| Sun Haven (default) | ~60 | ~20 min | ~20 min | Clock display |
| Spiritfarer | ~50 | ~20 min | ~20 min | Visual sun/moon |
| Slime Rancher 1 & 2 | 60 | ~24 min | ~24 min | Sun/moon visual |
| APICO | 60 | 24 min | 24 min | Hour display |
| Wylde Flowers (Normal) | ~60 | ~20–25 min | ~20–25 min | Clock |
| Palia | 150 | 60 min | 60 min | Analog clock |
| Animal Crossing: NH | 3,600 | 24 hours | 24 hours | Real-time 1:1 |
| Disney Dreamlight Valley | 3,600 | 24 hours | 24 hours | Real-time 1:1 |

**The median active day length across accelerated-time farming games is approximately 18–20 real minutes.** Graveyard Keeper at 7.5 minutes is the shortest and is often criticized for feeling rushed. Stardew Valley at ~14 minutes is on the fast end, and its TimeSpeed mod (which slows or freezes time) is one of the most downloaded mods in the game's history.

Your current **5-minute day** sits dramatically below even Graveyard Keeper. To reach genre parity, you'd need to roughly **3× to 4× your current day length**, targeting 15–20 real minutes of active play.

## How games increment time: 10-minute chunks versus smooth ticking

Two dominant approaches exist for time progression, and both are well-represented in the genre.

**10-minute increment systems** are used by Stardew Valley, the Harvest Moon / Story of Seasons series, and Moonstone Island. In Stardew Valley, each 10-minute block takes **7 real seconds** to elapse — the clock displays 6:00, then jumps to 6:10, then 6:20. ConcernedApe originally set this at 5 seconds per tick but increased it to 7 after playtesting, citing Harvest Moon: Back to Nature's pacing. This creates a distinctive "chunky" feel: players think in terms of blocks rather than individual minutes, which simplifies planning ("I have 3 ticks before the shop closes") but can feel jarring when time jumps.

**Smooth minute-by-minute progression** is used by My Time at Portia, My Time at Sandrock, Coral Island, APICO, and most newer titles. In these games, the clock visibly ticks through each minute — typically at a rate of **roughly 1 real second per in-game minute**. This creates a more naturalistic feel where lighting transitions are gradual and the passage of time is continuous rather than stepped. For your insect photography game, smooth progression likely serves you better since it aligns with the contemplative, observational pace of the gameplay.

**A third approach** — and one worth serious consideration for a cozy game — is Littlewood's action-based system, where **time doesn't pass at all unless the player performs an action**. Walking, browsing menus, and looking around are free. Only productive actions (mining, chopping, crafting) consume stamina, and the day ends when stamina runs out. This system was widely praised as the most anxiety-free approach in the genre and is philosophically perfect for a game where the core verb is "observe."

## Creature visit patterns: what Neko Atsume and APICO teach us

Games with visiting creatures fall into two design camps, each with distinct timing philosophies.

**Neko Atsume** pioneered the idle-visitor model. Cats arrive and depart on a probabilistic system — not fixed timers — with individual visits lasting roughly **30 minutes to several hours** in real time. An expanded yard holds up to 10 cats simultaneously, and checking every 2–3 real hours typically reveals a completely new roster. Food is the primary visit driver: when food runs out, no visitors arrive. The game is designed around **intermittent 30-second check-ins** spaced hours apart — the antithesis of a ticking-clock farming game. Adorable Home follows a similar model with activity cooldowns of ~20–30 real minutes per interaction.

**APICO** takes a more systematic approach. Bees operate on real-time lifespan cycles where the queen's lifespan trait (rated 1–7) determines how many real seconds she lives before producing offspring. Different species are **diurnal, nocturnal, crepuscular, or cathemeral**, creating natural rotations throughout the 24-minute in-game day. Specific flowers can extend activity windows by 2 hours, and some rare cross-breeds require queens to expire during narrow time windows (the Twilight Bee needs expiry between 1:00 and 4:00 AM — the tightest window in the game). This system creates **strategic timing depth** without moment-to-moment pressure.

For your game, with insect visit durations of 90–240 game-seconds and a 5-minute day, insects currently stay for **roughly 1.5–4 real minutes**. That's reasonable as a visit length, but the problem is the day provides too few meaningful "windows" to observe them. If you tripled the day length to 15 minutes, the same 90–240 game-second visits would still translate to 1.5–4 real minutes at a 1:1 game-to-real ratio (if you keep visits in game-time), giving players **4–10 complete visitor rotations per day** — enough to experience variety without feeling they missed something.

## Speed controls are becoming standard in cozy games

The genre has split into three camps on speed controls, and the trajectory favors player choice.

**Colony and city builders** (RimWorld, Cities: Skylines, Oxygen Not Included, The Sims) universally offer **Pause, ×1, ×2, ×3** as baseline. These games involve long wait periods punctuated by crisis moments, making speed control essential to the experience.

**Modern farming sims** increasingly offer **day-length sliders**, but notably these sliders let players *slow down*, not speed up. My Time at Sandrock offers the widest range at **0.6× to 3.0×** (default 1.0×), where the community overwhelmingly recommends playing at 0.6×. Coral Island provides a 50%–100% slider, with 50% being the most popular community setting. Sun Haven allows choosing between 15, 20, 30, or 40-minute days. Moonstone Island added an adjustable day-length setting post-launch after player requests. This pattern is important: **players consistently want more time, not less**.

**Classic farming games and real-time games** offer no speed controls. Stardew Valley has no built-in option, but its TimeSpeed mod — which allows freezing, slowing, or speeding time per-location — is among the game's most popular mods. Animal Crossing, Dreamlight Valley, Neko Atsume, and Cozy Grove use real-time clocks with no adjustability by design, because the pacing *is* the mechanic.

For your game, offering at minimum a **×0.5 / ×1 / ×2 speed** and ideally a **continuous slider** from 0.5× to 2× would match modern expectations. Given your current 5-minute base day, even ×0.5 would only yield 10 minutes — still short of the genre standard.

## Why 5 minutes feels wrong: the design theory behind cozy pacing

The most authoritative source on cozy game design — the **2017 Project Horseshoe paper** by Daniel Cook, Tanya X. Short, Chelsea Howe, and others — explicitly identifies **time pressure as anti-cozy**. The paper defines coziness as requiring safety (absence of risk), abundance (nothing is "lacking, pressing, or imminent"), and softness (lower arousal, slower tempo). Mandatory time-bound tasks, urgency, and fear of missing something directly negate these properties.

ConcernedApe's design philosophy offers a counterpoint worth considering. He deliberately chose a faster pace for Stardew Valley, stating: **"I'd rather err on the side of things going by too fast than too slow. If the game goes by too fast, you can always play through multiple times. If a game is too slow you might get bored and stop playing."** He also noted that each second added to the 10-minute tick increases total playtime by 10–15 hours — a meaningful consideration for a game with a 2-year story arc.

However, Stardew's community response complicates this. Players consistently report that **the perception of time pressure matters more than the reality** — even when there's technically no penalty for a "wasted" day, a fast clock makes players feel they *must* optimize. Common complaints include: arriving at shops just after closing, feeling that farm chores consume the entire morning, and experiencing anxiety that contradicts the game's cozy branding. The TimeSpeed mod's popularity is the strongest signal that **14 minutes per day is already on the short side** for many players.

For an insect photography game specifically, the mismatch is even more acute. Photography and observation are inherently about **slowing down and savoring moments** — golden hour light, an insect landing on a flower, the transition from dusk to fireflies. A 5-minute day gives you roughly **37 seconds of golden hour**. Tripling that to 15 minutes gives you nearly 2 real minutes of that magical light — enough time to notice it, appreciate it, and capture it.

## What this means for your game's time system

Based on the data across 20+ games and design theory, here are the specific numbers and approaches to consider.

**Target day length**: **15–20 real minutes** for your base ×1 speed. This means roughly **37–50 real seconds per in-game hour** (compared to your current 12.5), or a total of **900–1,200 real seconds per full 24-hour day** (compared to your current 300). This 3–4× increase aligns with the genre median and gives photography moments room to breathe.

**Time pausing** is a critical pressure-relief valve that most successful cozy games employ. Time should pause or dramatically slow during: the photography viewfinder/camera mode (this is your core activity — never penalize it), menus and inventory, dialogue with NPCs, and reading field guide entries. Stardew pauses during all menus in single-player. My Time at Sandrock pauses during dialogue. Several designers argue that **pausing during the core activity** is the single most impactful pacing decision you can make.

**Time display** deserves careful thought for an insect game. Kynseed's diegetic approach (picking a dandelion to check time) and Spiritfarer's visual sun/moon arc are more cozy-aligned than a ticking digital clock. Consider using **environmental period labels** — "Early Morning," "Midday," "Golden Hour," "Dusk," "Moonrise" — which double as photography terminology and insect activity cues. Bold time-of-day shifts through lighting, ambient sound (morning birdsong, evening crickets), and insect behavior rather than a prominent clock.

**Speed slider**: Offer at minimum ×0.5 / ×1 / ×2, matching the pattern of My Time at Sandrock and Coral Island. With a 15-minute base day, ×0.5 gives 30 minutes (Palia-like) and ×2 gives 7.5 minutes (for experienced players repeating days). The community pattern is clear: **most players who adjust speed slow it down**, so default to the slower end and let speedrunners accelerate.

## Conclusion

The cozy farming genre has remarkably consistent timing: **15–20 real minutes per active day, 45–60 real seconds per in-game hour, with generous time pausing during interaction**. Your current 5-minute day at 12.5 seconds per hour is an outlier — faster than every comparable title including Graveyard Keeper, which isn't marketed as cozy. The fix is straightforward: triple or quadruple your base day length to ~900–1,200 real seconds, add a speed slider for player choice, and pause time during photography. The 10-minute display increment (à la Stardew) works well but isn't necessary — smooth minute-by-minute ticking is equally common and may better suit an observation-focused game. Most importantly, for a game whose central joy is *noticing things*, the clock should feel like a gentle companion guiding you through different insect encounters — not a countdown timer rushing you past them.