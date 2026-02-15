# Project Flutter — Game Design Document
**Version:** 3.2  
**Date:** 2026-02-15  
**Moteur:** Godot 4.5 (C# 12 / .NET 8)  
**Développeuse:** Karianne (solo)  
**Plateformes:** Windows (Steam), potentiel Linux/Mac  
**Prix cible:** $7.99–$9.99  
**Durée de jeu:** 6–8 heures pour compléter le journal (100%)  
**Langues:** English + Français (UI + texte journal, aucun dialogue)

---

## 1. Vision

**Pitch en une phrase:** Cultive un jardin peint à la main pour attirer et photographier 72 espèces d'insectes dans un sim nature cozy sans pression.

**Pitch d'ascenseur:** Project Flutter est un jeu de jardinage top-down où tu plantes des fleurs et des herbes pour attirer de vrais insectes — abeilles, papillons, papillons de nuit, libellules, lucioles. Chaque plante attire des espèces spécifiques. Tu photographies les insectes pour les documenter dans un journal de terrain illustré à la main. Un cycle jour/nuit transforme ton jardin : abeilles et papillons le jour, papillons de nuit et lucioles la nuit. Équilibre la récolte de nectar pour ta monnaie et le maintien des fleurs en floraison pour attirer les espèces rares. Commence par un petit jardin et débloque des prairies, forêts, étangs et une serre tropicale secrète. Une agence de conservation locale reconnaît tes efforts à chaque jalon.

**Fantaisie fondamentale:** « J'ai créé ce beau jardin, et regarde qui est venu visiter. »

**Philosophie de design:** Project Flutter récompense l'observation, pas les réflexes. Chaque système — durées d'insectes, photographie, progression des plantes, économie — renforce la même fantaisie d'un photographe nature patient et attentif dont le jardin s'enrichit par le soin et la curiosité.

**Jeux comparables:**
- Neko Atsume (poser objets → créatures arrivent → les collectionner)
- APICO / Mudborne (journal nature, découverte par expérimentation)
- Stardew Valley (style visuel de référence, vue top-down légèrement inclinée)
- Kabuto Park (scope serré, collection d'insectes, 99% positif Steam)
- Pokémon Snap (système de photo à double axe)
- Viridi (entretien de plantes, croissance en temps réel)

---

## 2. Boucle de jeu

```
PLANTER graines → CULTIVER & ARROSER → ATTIRER insectes →
PHOTOGRAPHIER pour documenter → GAGNER nectar → ACHETER graines/zones/outils → RÉPÉTER
```

**Déroulement de session (20–45 min, soit ~1.5–3 cycles jour/nuit) :**
1. Vérifier le jardin — voir ce qui fleurit, quels insectes sont arrivés
2. Photographier les nouveaux insectes / reprendre ceux non documentés pour améliorer les étoiles
3. Récolter du nectar sur certaines fleurs (les rend temporairement moins attractives)
4. Acheter de nouvelles graines, outils ou bordures avec le nectar gagné
5. Arroser les plantes (ou laisser les sprinklers s'en occuper)
6. Attendre ou accélérer la croissance et les nouvelles arrivées
7. La nuit tombe — nouveaux insectes, nouvelles fleurs nocturnes
8. Photographier les espèces nocturnes (max 2★ sans lanterne, sauf firefly)
9. Changer de zone pour explorer d'autres habitats
10. Consulter le journal — herbier, progression conservation, bordures photo

---

## 3. Style artistique

- **Illustration numérique peinte à la main** (Paint Tool SAI, tablette graphique)
- **PAS du pixel art** — style illustré lisse et chaleureux, esthétique aquarelle/storybook
- **Vue top-down avec léger angle** (perspective Stardew Valley, ~30° d'inclinaison)
- **Palette de couleurs:** Tons chauds et naturels. Verts, bruns terreux, couleurs de fleurs pop contre le sol. Tons doux (verts, jaunes chauds, roses poussiéreux). Éviter les néons haute saturation.
- **Résolution:** Tiles de 64×64px, sprites peints à 128×128 (2× display) pour rendu net avec filtrage linéaire
- **Sprites d'insectes:** 32–48px en jeu, peints à 128×128. Animation par tweening de parties du corps (ailes, pattes, antennes) via AnimationPlayer. À cette taille, couleur et silhouette > détail fin.
- **Illustrations journal:** 256×256, détaillées — c'est la « récompense » artistique. 3 niveaux de rendu (voir §4.4)
- **Pipeline art:** Peindre les parties du corps en SAI → exporter en PNG 32bpp ARGB → assembler les animations dans Godot avec tweens → shader de vent pour les plantes
- **UI:** Propre et minimaliste. Esthétique cadre bois/naturel. Icônes plutôt que texte. Le journal est le hub central de l'interface.
- **Formes arrondies, bords doux, gradients doux** — les insectes doivent sembler grassouillets et approchables

---

## 4. Mécaniques de jeu

### 4.1 Grille de jardin
- Système de placement **snap-to-grid** avec tiles carrées (64×64px)
- Chaque tile contient : une plante, un objet d'infrastructure (eau, pierre, bûche, sprinkler), ou vide (sol/herbe)
- Plantes occupent 1×1 tile (exception : quelques plantes 2×2 exotiques)
- **Aucun personnage joueur** — interaction directe avec le curseur (clic pour planter, cliquer pour récolter)
- Curseurs personnalisés (petite truelle pour planter, appareil photo pour photographier)

### 4.2 Croissance des plantes
- Plantes poussent en **4 stades visuels:** Graine → Pousse → Croissance → Floraison
- Seules les plantes en **Floraison** attirent des insectes et activent leur aura (si niveau suffisant)
- Vitesse de croissance par rareté :
  - Common : ~2 cycles (60s)
  - Uncommon : ~3 cycles (90s)
  - Rare : ~4–5 cycles (120–150s)
- **Les plantes ne meurent JAMAIS** (design cozy, sans pression)
- **Les plantes non arrosées arrêtent complètement de pousser** — rien ne meurt, la croissance pause simplement. Le joueur qui oublie d'arroser revient et trouve des plantes qui n'ont pas bougé, pas un jardin mort.
- **Après récolte**, les plantes reviennent au stade Croissance et doivent reflorir :
  - Common : 1 cycle (30s) — récolte libre
  - Uncommon : 1.5 cycles (45s) — pause gérable
  - Rare : 2 cycles (60s) — planifier autour
- **Principe clé :** La repousse est toujours plus courte que la croissance initiale. Récolter ne doit jamais se sentir comme une punition.
- Animation de transition : shrink 80% → swap texture → bounce 110% → settle 100% + particules feuilles
- Shader de vent (vertex shader sine-based) pour animation de balancement, pas de frames supplémentaires. Offset par instance pour éviter la synchronisation.

### 4.3 Système de leveling des plantes (global par espèce)

Le leveling est **global par espèce** — "tu as récolté 15 lavandes au total dans ta vie → TOUTES les lavandes sont maintenant Niveau 3 pour toujours." Chaque plante plantée hérite automatiquement du niveau global de son espèce. L'aura s'active quand la plante est en phase de floraison.

| Niveau | Récoltes requises | Effet nectar | Aura | Badge feuille |
|--------|-------------------|-------------|------|---------------|
| 1 | 0 (base) | Nectar de base | Aucune | 🟤 Brun terre |
| 2 | 5 récoltes | +25% nectar | Aucune | 🟢 Vert tendre |
| 3 | 15 récoltes | +25% nectar | 1 case radius | 🔵 Bleu ciel |
| 4 | 30 récoltes | +25% nectar | 2 cases radius | 🟣 Violet |
| 5 | 50 récoltes | Nectar doublé | 3 cases radius | 🟡 Doré |

**Aura de plante (en floraison uniquement):** Le type d'aura dépend de la famille de plante. Les auras en floraison offrent des bonus passifs aux cases adjacentes dans le rayon. (Familles et effets exacts à déterminer au sprint balancement.)

**Badge feuille :** Petit symbole de feuille coloré à côté du nom de la plante dans le journal (herbier) et dans le tooltip au survol dans le jardin. Badge doré au Niveau 5 = satisfaction pure.

### 4.4 Mécanique de photographie

**Mode photo :** Basculer en mode photo (toggle ou maintenir une touche). Cliquer et maintenir sur un insecte pour commencer la mise au point. Un cercle concentrique se ferme autour de l'insecte (1–2 secondes, ease-in-quint pour feeling satisfaisant). L'insecte continue de bouger pendant la mise au point.

**Pause monde en mode photo :** Quand le mode photo est actif, le **cycle jour/nuit gèle** (le soleil s'arrête, la lumière ne change pas) et les **timers de visite des insectes sont suspendus** (aucun insecte ne partira pendant que tu photographies). Cependant, les **insectes continuent leur pattern de mouvement** — un Monarch qui flutter reste un défi à cadrer, un Crawl reste plus facile. Ceci préserve le skill de tracking photo sans jamais pénaliser le joueur pour avoir utilisé l'activité principale du jeu. Inspiré de Pokémon Snap et Penko Park où les sujets bougent toujours pendant le cadrage.

**Quand le cercle se ferme : son d'obturateur + flash blanc bref + insecte freeze momentanément.**

**Qualité photo — 3 niveaux d'étoiles :**
- ★☆☆ — insecte près du bord (documenté, entrée basique)
- ★★☆ — insecte raisonnablement centré
- ★★★ — centrage parfait

**Système de 3 niveaux visuels dans le journal :**
Chaque insecte a une seule illustration, affichée à différents niveaux de rendu selon la meilleure photo prise :
- ★☆☆ → **Esquisse pâle** (illustration désaturée, traits légers)
- ★★☆ → **Aquarelle partielle** (couleurs partielles, certains détails manquants)
- ★★★ → **Illustration vibrante** (art complet, couleurs riches, tous les détails)

**Zéro art supplémentaire par niveau** — c'est la même illustration avec des filtres d'opacité/saturation différents. Les complétionnistes veulent atteindre 3 étoiles pour chaque espèce.

Première photo réussie d'une espèce = **nouvelle entrée journal** (récompense principale). Peut rephotographier pour améliorer le classement étoiles.

**Photographie de nuit :**
- Sans lanterne : maximum **2★** pour tous les insectes nocturnes (photos sombres, journal en esquisse/aquarelle seulement)
- Avec lanterne de jardin : **3★** possible pour tous les nocturnes
- **Exception — Firefly :** Seul insecte pouvant atteindre 3★ sans lanterne, à condition de photographier pendant son pulse lumineux (timing). Si la lanterne est active, le firefly est "lavé" par la lumière → plafonné à 2★. Il faut éteindre la lanterne (toggle on/off) pour la photo parfaite. Micro-décision satisfaisante.

**Patterns de comportement affectent la difficulté** selon les 7 types de mouvement : Hover, Flutter, Crawl, Erratic, Dart, Skim, Pulse.

**Mécanique de fuite photo :** Après chaque tentative de photo qui ne donne pas 3★ (ou miss), l'insecte a un pourcentage de chance de fuir, scalé par rareté. Un 3★ réussi ne déclenche jamais de jet de fuite — le skill est récompensé. Inspiré du système de fuite des Pokémon légendaires : la rareté crée une tension réelle où chaque essai est un gamble.

| Rareté | % fuite par essai raté | Essais moyens avant fuite |
|--------|----------------------|--------------------------|
| Common | 15% | ~6-7 |
| Uncommon | 25% | ~4 |
| Rare | 40% | ~2-3 |
| Very Rare | 60% | ~1-2 |
| Legendary | 75% | ~1.3 |

**Bonus aura plante level 3+ :** Dans le rayon d'aura d'une plante level 3+, le % de fuite est réduit de **15 points** (pas de plancher). Un Legendary sur plante level 3+ = 60% au lieu de 75%. Un Common sur plante level 3+ = 0% (ne fuit jamais — récompense cozy pour l'investissement dans les plantes).

**Comportement de fuite :** 🧪 *À tester en implémentation :* fuite instantanée (plus de tension, style Pokémon) vs soft warning (ailes rapides 5-10s, plus cozy). Le soft warning pourrait être gardé pour les départs naturels (fin de timer de visite) mais retiré pour la fuite post-photo.

**Le juice stack — feedback proportionnel à la qualité :**

| Qualité | Feedback |
|---------|----------|
| ★☆☆ | Shutter *click* chaud + cercle collapse doux + photo glisse dans le coin journal |
| ★★☆ | *Ka-chick* plus fort + petit puff de pollen + cercle pulse doré |
| ★★★ | Shutter résonant + **500ms freeze frame** + particules dorées + chime musical deux notes + stamp journal avec bounce tween |
| Nouvelle espèce | Shutter mécanique riche + **500ms freeze** + bloom bord d'écran + burst de particules + phrase musicale complète + animation page-flip + bannière "NOUVELLE ESPÈCE" |

### 4.5 Système de spawn d'insectes
- Chaque plante en floraison a un nombre limité de **slots d'insectes** (1–3 selon plante)
- Toutes les quelques secondes, le système vérifie chaque plante en floraison et peut spawner un insecte si :
  - Il y a un slot libre
  - Les conditions de l'insecte sont remplies (heure, plantes requises, zone, mécaniques spéciales)
  - Le jet de dé passe le poids de rareté de l'insecte
- **Poids de spawn par rareté:** Common 45, Uncommon 25, Rare 12, Very Rare 4, Legendary 1
- **Durées de visite révisées** (×10–20 par rapport aux valeurs action-game) :

| Rareté | Durée de visite | Rationale |
|--------|----------------|-----------|
| Common | **2–5 minutes** | Présence fiable et relaxante, amplement le temps de composer |
| Uncommon | **1–3 minutes** | Notablement plus court mais jamais paniquant |
| Rare | **45s–2 minutes** | Moment "oh, c'est spécial!" avec le temps de réagir posément |
| Very Rare | **30–90 secondes** | Excitant et urgent mais suffisant pour une photo bien composée |
| Legendary | **30–60 secondes** | Adrénaline maximale, une fenêtre pour une bonne photo |

- **Indices de départ :** Pas de disparition abrupte. Les insectes affichent des indices — ailes qui battent plus vite, l'insecte décolle et tourne dans le jardin 5–10 secondes supplémentaires. "Soft warning" au lieu de timer.
- **Son de rare :** Un chime doux ou buzz distinctif quand un insecte rare/legendary arrive — alerte sans alarme.
- **Cap de population** par zone (8–15 insectes visibles selon taille de zone)
- **Slot visiteur spécial :** Un slot dédié pour les insectes rares/legendary qui ne compétitionne pas avec les spawns communs.
- Conditions de spawn spéciales :
  - `PlantAttraction` : l'insecte apparaît près de plantes spécifiques
  - `WaterRequired` : nécessite des tuiles d'eau dans la zone
  - `MinInsectsPresent` : apparaît quand N+ insectes sont déjà actifs (prédateurs)
  - `MultiPlantCombo` : nécessite plusieurs plantes spécifiques en floraison
  - `MinPlantDiversity` : nécessite N+ espèces de plantes différentes
  - `DecomposingWood` : nécessite des bûches à un certain stade de décomposition
  - `SunTrapRock` : nécessite des pierres chauffées par le soleil
  - `UVLamp` : nécessite une lampe UV placée (pour papillons de nuit)

### 4.6 Journal de terrain (hub central)

Le journal est l'**interface unique centrale** du jeu. Un seul bouton l'ouvre, des signets latéraux (style Monster Train) permettent de naviguer entre sections. L'animation de "tourner les pages" entre les sections donne du poids au journal comme objet.

**Structure du journal :**

| Signet | Contenu |
|--------|---------|
| 📸 **Insectes** | Pages par zone, entrées photo avec 3 niveaux visuels, silhouettes grises pour non-découverts |
| 🌱 **Herbier** | Niveaux des plantes, barres de progression, badges feuille colorés, auras |
| 🎨 **Collection** | Bordures de photos débloquées, statistiques, cosmétiques |
| 🏛️ **Conservation** | Jalons de l'agence, certificats, récompenses de progression |
| ⚙️ **Réglages** | Son, contrôles, langue, plein écran |

**Détail section Insectes :**
- Grille d'entrées : espèces découvertes montrent des portraits illustrés au niveau de qualité atteint ; non découvertes montrent des silhouettes grises
- Chaque entrée contient : nom (EN/FR), illustration (3 niveaux), classement étoiles, texte de saveur / fun fact, indice d'habitat, date de première découverte, zone et période d'activité
- Indices de découverte pour espèces non découvertes (indices vagues aux jalons)
- Compteur de complétion : « 37/72 espèces documentées » avec pourcentage
- Organisation par zone avec onglets, filtrage par catégorie

**Détail section Herbier :**
- Mini-entrée par plante : nom, niveau actuel, barre de progression vers le prochain niveau, description de l'aura
- 4–6 plantes par page, compact mais utile comme référence
- Utilise les mêmes sprites de plantes déjà existantes — peu d'art supplémentaire

### 4.7 Monnaie & Économie
- **Monnaie unique : Nectar**
- Gagné par : récolte de fleurs en floraison (clic sur plante → récolter nectar → plante revient au stade Croissance)
- **Tension fondamentale :** Récolter donne du nectar pour acheter graines et débloquer zones, MAIS la plante arrête de fleurir temporairement = moins d'insectes = plus dur de photographier
- **ROI cible : 2× en 2–3 récoltes** pour les graines communes
- **État initial :** Le joueur reçoit **25 nectar** (assez pour 4 graines communes)

**Coûts des graines :**

| Rareté | Coût (nectar) | Rendement | ROI |
|--------|---------------|-----------|-----|
| Common | 5–10 | 3 par récolte | 2× en 2 récoltes |
| Uncommon | 15–30 | 5 par récolte | 2× en 3 récoltes |
| Rare | 40–75 | 8–10 par récolte | 2× en 4 récoltes |

**Sources bonus de nectar (accélèrent ~20%) :**
- Première entrée journal : +5 nectar (unique par espèce)
- Photo 3 étoiles : +3 nectar (par photo unique)
- Fin de journée : +2 nectar (chaque jour de jeu)
- Entrées journal suivantes : +2 nectar chacune

**Aucune transaction en argent réel.** Le nectar est la seule monnaie.

### 4.8 Nectar sinks

**Tier 1 — Progression principale :**
- Déblocage de zones (voir §4.10)
- Graines de plantes (toutes raretés)
- Lanterne de jardin (~50 nectar, achat unique, améliore photos nocturnes à 3★)

**Tier 2 — Qualité de vie — Sprinklers :**

| Niveau | Pattern | Cases arrosées | Coût |
|--------|---------|---------------|------|
| Manuel | 1 case à la fois | 1 | Gratuit |
| Sprinkler I | 3×3 centré | 8 (+ case occupée) | ~40 nectar |
| Sprinkler II | 5×5 centré | 24 (+ case occupée) | ~120 nectar |
| Sprinkler III | 7×7 centré | 48 (+ case occupée) | ~300 nectar |

Les sprinklers sont **passifs** — une fois placés, les plantes dans leur rayon sont toujours arrosées. Pas de timer, pas d'activation. Le sprinkler est un objet statique : les plantes dans son rayon n'ont simplement jamais soif. C'est la récompense : tu as payé, maintenant tu es libre de te concentrer sur la photo.

Le Sprinkler III (7×7) couvre presque entièrement la Meadow (6×6) — moment "j'ai automatisé mon jardin" très satisfaisant.

**Tier 3 — Cosmétiques (sink infini) — Bordures de photos :**
- 8–12 bordures décoratives pour les photos du journal
- 2–3 gratuites de base, les autres achetables
- Packs thématiques : "Nature" (feuilles, vignes), "Saisons" (flocons, cerisier), "Doré" (premium)
- Coût : ~15–50 nectar chacune
- Chaque joueur personnalise son journal différemment
- Facile à produire côté art (cadres 2D décoratifs)
- Ne bloque jamais la progression

### 4.9 Cycle jour/nuit

**Durée du jour : 15 minutes réelles par cycle complet (×1).** Basé sur une recherche de 20+ jeux du genre — la médiane du farming/cozy est 15–20 min. Le design précédent de 5 min était 3× plus rapide que le jeu le plus rapide du genre (Graveyard Keeper à 7.5 min). Pour un jeu de photographie et d'observation, le temps doit permettre de *savourer* chaque période — crépuscule, nuit aux lucioles, aube.

**Progression du temps :** Smooth minute-by-minute (pas d'incréments de 10 min style Stardew). Chaque seconde réelle ≈ 1.6 minutes en jeu. Transitions de lumière graduelles et naturelles.

**Périodes du jour et palette de l'horloge :**

| Période | Heures jeu | Durée réelle (×1) | Couleur horloge | Gameplay |
|---------|-----------|-------------------|----------------|----------|
| 🌅 Aube | 5h–7h | ~1 min 15s | Rose-orangé doux | Transition, insectes crépusculaires s'activent |
| ☀️ Matin | 7h–12h | ~3 min 07s | Jaune doré clair | Diurnes actifs, rosée, lumière fraîche |
| 🌤️ Après-midi | 12h–17h | ~3 min 07s | Bleu ciel chaud | Pic d'activité, pleine lumière |
| 🌇 Crépuscule | 17h–19h | ~1 min 15s | Violet-ambre | Transition magique, crépusculaires |
| 🌙 Nuit | 19h–5h | ~6 min 15s | Indigo → bleu nuit | Nocturnes, lucioles, moths, calme |

La nuit représente ~42% du cycle — c'est une vraie phase de jeu, pas un interlude. Avec 16 espèces nocturnes et 5 crépusculaires, la nuit est riche en contenu.

**Horloge analogique HUD :**
- Cadran circulaire avec **12 segments de 2h chacun**, colorés selon la période
- **Bandes de couleur nettes avec séparateurs fins** entre chaque segment (testé : le léger fondu rendait l'horloge illisible à petite taille)
- Une **aiguille unique** fait un tour complet en 24h de jeu (15 min réelles)
- **Style visuel : bois patiné ou laiton antique** — à tester lors du Sprint 6 (art). Les deux collent à l'esthétique nature/journal de terrain
- L'aiguille pourrait être une petite branche ou tige de plante
- **Pas d'affichage numérique** — le joueur lit l'heure visuellement par la position de l'aiguille dans la bande de couleur
- Le joueur comprend intuitivement : "je suis dans le violet, la nuit indigo arrive" → préparation naturelle

**Contrôle de vitesse — intégré à l'horloge :**
- **Clic sur l'horloge** pour cycler entre ×0.5 / ×1 / ×2
- ×0.5 → 30 min/jour (mode contemplatif, style Palia)
- ×1 → 15 min/jour (défaut)
- ×2 → 7.5 min/jour (joueurs expérimentés, accélérer l'attente)
- L'aiguille tourne visiblement plus vite/lent selon la vitesse
- **Indicateur de vitesse au centre du cadran :** chiffre blanc sur cercle brun — ".5", "1", ou "2". Cliquable pour cycler. (Testé : le triangle ▶ n'était pas assez lisible à cette taille, le chiffre seul est plus clair.)
- **Les joueurs veulent majoritairement ralentir, pas accélérer** (données Coral Island, My Time at Sandrock, mods Stardew). Le ×0.5 est une option importante.

**Système de pause du temps :**
- **Mode photo actif** → cycle jour/nuit gelé, timers de visite gelés, insectes continuent de bouger (voir §4.4)
- **Journal ouvert** → pause complète (monde + insectes)
- **Shop ouvert** → pause complète
- **Menus / réglages** → pause complète
- En pratique, le joueur a *plus* que 15 min de temps utile par jour grâce aux pauses

**Impact gameplay :**
  - **Insectes diurnes (51):** abeilles, papillons, coccinelles, libellules
  - **Insectes nocturnes (16):** papillons de nuit, lucioles, grillons
  - **Insectes crépusculaires (5):** chrysope, lucane, sphinx de la vigne
  - **Plantes nocturnes** s'ouvrent seulement la nuit (onagre, etc.)
  - Les plantes diurnes se ferment la nuit (mais ne perdent pas de progrès)
  - **Photographie nocturne :** Max 2★ sans lanterne, 3★ avec lanterne (sauf firefly — voir §4.4)
- **Lanterne de jardin :** Achat unique (~50 nectar), toggle on/off. Améliore la luminosité de nuit pour photos 3★. Toggle off pour la mécanique firefly.
- **Transition visuelle :** CanvasModulate avec changement graduel de couleur, éclairage, ombres. Les transitions suivent les bandes de l'horloge — quand l'aiguille entre dans le violet, la lumière vire au crépuscule.
- **Audio ambiant :** chant d'oiseaux (matin) → bourdonnement d'insectes (après-midi) → grillons/grenouilles (crépuscule/nuit) → chorus de l'aube
- **Pas de streaks ni de contenu quotidien manquable.** L'horloge est interne au jeu, pas de pression sur le joueur de revenir à des moments précis.

### 4.10 Progression par zones

**7 zones + 1 zone secrète, déverrouillage progressif (double-gate : babillard de conservation + nectar dépensé) :**

| # | Zone | Grille | Photos babillard | ⭐ requises | Coût nectar | Espèces |
|---|------|--------|-----------------|-------------|-------------|---------|
| 1 | Starter Garden 🌱 | 5×5 | — | — | Gratuit | 14 |
| 2 | Meadow 🌻 | 6×6 | 5 (2 spéc. + 3 libres) | 8 (moy. 1.6★) | 100 | 11 |
| 3 | Forest 🌲 | 6×6 | 6 (2 spéc. + 4 libres) | 11 (moy. 1.8★) | 200 | 11 |
| 4 | Deep Wood 🪵 | 5×5 | 7 (3 spéc. + 4 libres) | 14 (moy. 2.0★) | 350 | 9 |
| 5 | Rock Garden ⛰️ | 5×5 | 8 (3 spéc. + 5 libres) | 18 (moy. 2.25★) | 500 | 9 |
| 6 | Pond 🐸 | 5×5 | 8 (3 spéc. + 5 libres) | 20 (moy. 2.5★) | 700 | 8 |
| 🔒 | Tropical Greenhouse 🌺 | 7×7 | — | — | 1000 | 10 |

**Ratio design :** Chaque zone demande ~30-40% des espèces disponibles dans les zones précédentes. Calibré contre Stardew Valley (6 salles, ~110 items, jeu de 50-170h), ajusté pour un jeu de 6-8h.

**Babillard de conservation — déblocage inspiré des bundles de Stardew Valley :**

Le simple "bouton acheter zone" est remplacé par un **babillard de conservation** avec des photos à épingler. Le babillard est un objet physique visible dans le Starter Garden (décor + trophée) qui se remplit au fil du temps. L'action d'épingler se fait depuis la **section Conservation du journal**, accessible de n'importe quelle zone — pas besoin de retourner au Starter.

Chaque zone à débloquer a une page dans le journal avec :
- **Slots spécifiques :** Silhouettes identifiables d'espèces précises (thématiquement liées à la zone suivante). Le joueur reconnaît la forme et sait quoi chercher.
- **Slots libres :** Cadres vides avec "?" — n'importe quelle espèce documentée.
- **Budget d'étoiles total :** Pas de minimum par slot individuel. Le joueur choisit *où* exceller — photographier parfaitement un Hover facile compense un 1★ sur un Dart difficile. Compteur visible "⭐ 8/11" dans la page Conservation.
- **Double-gate maintenu :** Babillard complet + coût nectar requis. L'économie reste pertinente.

**Animation de déverrouillage :**
- Dernière photo épinglée → tampon de certification dans le journal + confettis
- Fermer le journal → sentier de fleurs qui pousse en accéléré vers la nouvelle zone
- Tab zone dans le HUD passe de grisé à coloré avec sparkle

La **Tropical Greenhouse est cachée** — pas visible dans les tabs de zone, pas mentionnée — jusqu'à ce que le joueur atteigne 75% du journal (54/72 espèces). À ce moment, la zone apparaît comme surprise. Pas de babillard pour la Tropical — c'est un milestone de conservation automatique (voir §4.11).

Le joueur peut naviguer librement entre les zones débloquées via des **onglets horizontaux** en haut de l'écran (signets avec icônes). Zones verrouillées sont cliquables mais ouvrent la page babillard du journal.

### 4.11 Narrative — Agence de conservation

**Pas de dialogue. Pas de PNJ actif. Pas de cutscenes.** Juste du texte statique dans le journal.

**Concept :** Une agence locale de conservation des insectes te contacte pour documenter les espèces de la région. Chaque jalon du journal déclenche une reconnaissance + récompense.

| Jalon | % Journal | Entrées | Récompense |
|-------|-----------|---------|------------|
| Certificat Bronze 🥉 | 25% | 18/72 | Titre + bordure de photo exclusive "Bronze Naturalist" |
| Certificat Argent 🥈 | 50% | 36/72 | Titre + bordure exclusive + bonus nectar conséquent |
| Certificat Or 🥇 | 75% | 54/72 | Titre + bordure exclusive + **Tropical Greenhouse débloquée** |
| Certificat Platine 💎 | 100% | 72/72 | Récompense finale (effet visuel spécial, insecte secret?, scène de crédits) |

Les certificats apparaissent dans la section Conservation du journal. Chaque jalon est une page spéciale avec le certificat illustré.

**Ton :** Professionnel mais chaleureux. "La Société entomologique est fière de reconnaître votre contribution à la documentation de la biodiversité locale." Pas de sarcasme, pas d'urgence — juste de la reconnaissance sincère.

---

## 5. Zones — Détail

### Zone 1 — Starter Garden 🌱
*Cottage garden, plates-bandes fleuries, palissade — zone tutoriel*

**14 espèces** (11 Common, 3 Uncommon) | 5–6 plantes
- Papillons : Cabbage White, Orange Tip, Red Admiral (Uncommon)
- Coléoptères : Seven-Spot Ladybug, 22-Spot Ladybug (jaune), Golden Tortoise Beetle (Uncommon), Rose Chafer, Japanese Beetle
- P. de nuit : Rosy Maple Moth, Garden Tiger Moth
- Autres : Marmalade Hoverfly, Green Lacewing (crépuscule), European Mantis (Uncommon), Western Honeybee
- **Plantes :** Lavande, Tournesol, Marguerite, Œillet d'Inde, Souci, Onagre (nuit)
- **Mécanique :** Aucune spéciale — apprentissage pur. Beaucoup de découvertes faciles.
- **Palette :** Verts, blancs, rouges, or, rose — jardin classique et accueillant

### Zone 2 — Meadow 🌻
*Prairie de fleurs sauvages, herbes dorées, espace ouvert, vent*

**11 espèces** (4 Common, 6 Uncommon, 1 Rare) | 5–6 plantes
- Papillons : Clouded Sulphur, Common Blue, Monarch (Uncommon, milkweed obligatoire), Gulf Fritillary (Uncommon), Old World Swallowtail (Rare)
- P. de nuit : Cinnabar Moth
- Abeilles : Wool Carder Bee (Uncommon), Long-Horned Bee (Uncommon)
- Sauterelles : Meadow Grasshopper, Band-Winged Grasshopper (Uncommon)
- Coléoptères : Six-Spotted Tiger Beetle (Uncommon)
- **Plantes :** Asclépiade (milkweed), Verge d'or, Trèfle, Bleuet, Séneçon, Chardon
- **Mécanique :** Shader de vent plus prononcé. Combinaisons de plantes pour Uncommon.
- **Palette :** Jaunes, oranges, violets, émeraude — couleurs prairie

### Zone 3 — Forest 🌲
*Lumière tamisée, mousse, fougères, champignons, sous-bois*

**11 espèces** (3 Common, 5 Uncommon, 3 Rare) | 4–5 plantes
- Papillons : Eastern Tiger Swallowtail (Uncommon), Question Mark (Rare)
- P. de nuit : Peppered Moth, Luna Moth (Rare), Elephant Hawk-Moth (Uncommon, crépuscule)
- Coléoptères : Stag Beetle (Uncommon, crépuscule), Ebony Jewelwing (Uncommon)
- Autres : Walking Stick (Rare, nuit, camouflage), Annual Cicada (Uncommon), Spotted Lanternfly
- Abeilles : Mason Bee
- **Plantes :** Fougère, Muguet, Digitale, Violette, Chèvrefeuille
- **Mécanique :** Patches de lumière filtrée (certains insectes préfèrent l'ombre). Nuit riche (3 espèces nocturnes).
- **Palette :** Verts sombres, bruns, éclats de lime/rose — lumière tamisée

### Zone 4 — Deep Wood 🪵
*Bûches moussues, compost, champignons, décomposition*

**9 espèces** (2 Common, 2 Uncommon, 2 Rare, 2 Very Rare, 1 Legendary) | 2–3 plantes + bûches/compost
- P. de nuit : Polyphemus Moth (Rare), Cecropia Moth (Very Rare), Death's-head Hawkmoth (Very Rare)
- Coléoptères : Rhinoceros Beetle (Rare, nuit), Colorado Potato Beetle, Weevil/Myllocerus, Firefly (Uncommon, nuit)
- Abeilles : Carpenter Bee (Uncommon)
- Autres : Leaf Insect (Legendary, nuit, mimétisme)
- **Plantes :** Champignons (décor), Mousse. **Bûches à 3 stades de décomposition** (frais → moisi → pourri) + **tas de compost** à retourner
- **Mécanique :** Les insectes sont attirés par le bois mort et la décomposition, pas les fleurs. 5 espèces nocturnes — zone idéale pour exploration de nuit.
- **Palette :** Bruns, verts mousse, éclats bioluminescents

### Zone 5 — Rock Garden ⛰️
*Pierres empilées, fleurs alpines en coussin, ciel ouvert, lichens*

**9 espèces** (3 Common, 3 Uncommon, 2 Rare, 1 Very Rare) | 3–4 plantes + pierres/crevasses
- Papillons : Black Swallowtail (Uncommon), Glasswing (Very Rare, ailes transparentes!)
- Coléoptères : Sacred Scarab (Rare)
- Abeilles : Teddy Bear Bee (Rare), Leafcutter Bee (Uncommon)
- Sauterelles : Cone-Headed Grasshopper (Uncommon, nuit), Field Cricket (Common, nuit, mécanique sonore)
- Autres : Pillbug (Common, se roule en boule), Ant (Common, piste en file)
- **Plantes :** Thym, Edelweiss, Saxifrage, Lavande de mer
- **Mécanique :** Pierres créant des **pièges à soleil** (certains insectes s'y chauffent). Crevasses à explorer. Le Grillon a une mécanique sonore (l'entendre avant de le voir).
- **Palette :** Gris pierre, lichens jaunes, fleurs alpines, noir brillant

### Zone 6 — Pond 🐸
*Nénuphars, roseaux, eau calme, reflets*

**8 espèces** (4 Common, 3 Uncommon, 1 Rare) | 2–3 plantes aquatiques + tuiles d'eau
- Libellules : Blue Dasher, Twelve-Spotted Skimmer, Flame Skimmer (Uncommon), Halloween Pennant (Uncommon), Red-Veined Darter (Uncommon), Emperor Dragonfly (Rare)
- Demoiselles : Azure Damselfly
- Autres : Water Strider
- **Plantes :** Nénuphar, Quenouille, Iris d'eau
- **Mécanique :** **Tuiles d'eau** comme terrain (CellType.Water, immutable). Animation eau (sine-wave + ripples). Water Strider et libellules dépendent de l'eau, pas des plantes. 100% diurne. **Bonus photo reflet** pour photos prises au-dessus de l'eau.
- **Palette :** Bleus, verts aqua, ambre, rouge feu

### Zone 7 — Tropical Greenhouse 🌺 (SECRÈTE — débloquée à 75% journal)
*Serre vitrée, orchidées, vignes, brume — zone endgame*

**10 espèces** (0 Common, 0 Uncommon, 2 Rare, 4 Very Rare, 4 Legendary) | 4–5 plantes exotiques
- Papillons : Zebra Longwing (Rare), Ulysses Butterfly (Very Rare), Queen Alexandra's Birdwing (Legendary)
- P. de nuit : Atlas Moth (Very Rare), Comet Moth (Legendary), Madagascan Sunset Moth (Very Rare)
- Abeilles : Orchid Bee (Very Rare), Blue-Banded Bee (Rare)
- Coléoptères : Hercules Beetle (Legendary, nuit)
- Autres : Orchid Mantis (Legendary)
- **Plantes :** Orchidée, Passiflore, Hibiscus, Lantana, Bougainvillier
- **Mécanique :** **Brume** (effet visuel), **lampe UV** pour papillons de nuit, **chrysalides** à observer (mini-jeu d'attente), **station de fruits** pour attirer coléoptères. Toutes les espèces sont Rare+ — zone spectacle.
- **Palette :** Tropicaux saturés — bleus, verts, roses, or, arc-en-ciel

---

## 6. Contenu

### 6.1 Espèces — Résumé (72 total)

| Catégorie | Espèces | % |
|-----------|---------|---|
| Papillons | 15 | 21% |
| Papillons de nuit | 12 | 17% |
| Coléoptères | 13 | 18% |
| Libellules & Demoiselles | 7 | 10% |
| Abeilles | 9 | 12.5% |
| Sauterelles & Grillons | 4 | 5.5% |
| Syrphes | 1 | 1.5% |
| Autres (mantis, phasme, etc.) | 8 | 11% |
| Isopode + Insecte social | 2 | 3% |
| **Total** | **72** | **100%** |

**Distribution de rareté :** 27 Common (37.5%) / 22 Uncommon (30.6%) / 11 Rare (15.3%) / 7 Very Rare (9.7%) / 5 Legendary (6.9%)

**Activité :** 51 diurnes (71%) / 16 nocturnes (22%) / 5 crépusculaires (7%)

*Référence canonique complète : voir Species Registry v3.0*

### 6.2 Plantes (~30 total)

| Zone | Plantes | Exemples | Rôle |
|------|---------|----------|------|
| Starter | 5–6 | Lavande, Tournesol, Marguerite, Œillet d'Inde, Souci, Onagre | Apprentissage, insectes Common |
| Meadow | 5–6 | Asclépiade, Verge d'or, Trèfle, Bleuet, Séneçon, Chardon | Combinaisons pour Uncommon |
| Forest | 4–5 | Fougère, Muguet, Digitale, Violette, Chèvrefeuille | Plantes d'ombre, moths |
| Deep Wood | 2–3 | Champignons, Mousse + Bûches + Compost | Insectes de décomposition |
| Rock Garden | 3–4 | Thym, Edelweiss, Saxifrage, Lavande de mer | Plantes résistantes |
| Pond | 2–3 | Nénuphar, Quenouille, Iris d'eau | Support aquatique |
| Tropical | 4–5 | Orchidée, Passiflore, Hibiscus, Lantana, Bougainvillier | Haute valeur, Rare+ |
| **Total** | **~28–32** | | |

**Mécaniques d'attraction non-plantes (~40% des insectes) :**
- Tuiles d'eau (14% des insectes — libellules, gerris)
- Bûches/compost en décomposition (11% — coléoptères de bois mort, Leaf Insect)
- Pierres chauffées par le soleil (5% — scarabée, grillon)
- Lampe UV / drap blanc (7% — papillons de nuit rares dans toutes les zones la nuit)
- Prédateurs suivant les proies (3% — Mantis apparaît quand 5+ insectes actifs)

### 6.3 Liste des assets art (estimée)

| Type d'asset | Quantité | Taille | Notes |
|--------------|----------|--------|-------|
| Plantes stades de croissance (4 × ~30) | ~120 | 128×128 | Peints en SAI, shader de vent |
| Insectes sprites jardin (body parts × 72) | ~220 | 128×128 | Tweened body parts, 2–4 pièces par insecte |
| Insectes illustrations journal (72) | 72 | 256×256 | Détaillées, la « récompense » art |
| Insectes silhouettes (72) | 72 | 256×256 | Versions grises du journal art |
| Bordures de photo (8–12) | ~10 | Divers | Cadres décoratifs 2D |
| Sprinklers (3 niveaux) | 3 | 64×64 | Simple |
| Lanterne de jardin | 1 | 64×64 | Toggle on/off |
| Badges feuille (5 niveaux) | 5 | ~16×16 | Petites icônes colorées |
| Certificats conservation (4) | 4 | ~256×256 | Pages journal spéciales |
| Fonds de zone (7) | 7 | Plein écran | Starter, Meadow, Forest, Deep Wood, Rock, Pond, Tropical |
| Tile sprites (sol, herbe, eau, pierre, bois) | ~25 | 64×64 | Réutilisables selon zones |
| Éléments UI (boutons, cadres, icônes, signets) | ~35 | Divers | Journal, shop, HUD, tabs zone |
| Horloge analogique HUD | 1 | ~128×128 | Cadran bois/laiton, 12 segments colorés nets, aiguille branche/tige |
| Art de curseur (2 modes) | 2 | ~32×32 | Truelle, appareil photo |
| Animation cercle photo | 1 | Programmatique | Cercle concentrique + flash |
| **Total assets uniques** | **~577** | | |

**Estimation de production art :** 160–270 heures au total
- Insectes (sprites + journal) : 80–120h
- Plantes (120 stades) : 40–60h
- Fonds + tiles + UI + cosmétiques : 40–90h

---

## 7. Audio

### 7.1 Musique
- **Paysages sonores ambiants inspirés ASMR** plutôt que musique de jeu traditionnelle
- **Système en couches :** ambiance de base + couches insectes activées selon contenu du jardin
- Jour : vent doux, chant d'oiseaux distant, boucles de guitare acoustique douce
- Nuit : grillons, chœur de grenouilles, hululements, piano/harpe doux
- Transition : fondu enchaîné graduel entre couches jour et nuit

### 7.2 Effets sonores
- **Interaction jardin :** sons de terre satisfaisants pour planter, splash d'eau pour arroser, pop doux pour récolter nectar
- **Photographie :** Le son d'obturateur est l'action la plus répétée du jeu — il doit être parfait. 3 couches audio : clic mécanique chaud (toujours), note musicale proportionnelle à la qualité (plus aigu = meilleure photo), son nature unique pour nouvelle espèce. Ronronnement de mise au point, son d'avancement de film.
- **Insectes :** sons ambiants spécifiques par espèce (buzz d'abeille, chant de grillon, scintillement de luciole, stridulation de cigale). Après une bonne capture, les sons ambiants proches se gonflent brièvement.
- **UI :** clics de bois doux pour boutons, tournement de page pour journal, carillon doux pour nouvelle découverte
- **Fanfare de découverte :** jingle spécial quand une nouvelle espèce est documentée
- **Son de visiteur rare :** chime doux quand un insecte rare/legendary arrive dans le jardin

---

## 8. Architecture technique

### 8.1 Stack technologique
- **Moteur :** Godot 4.5
- **Langage :** C# 12 / .NET 8 (aucun GDScript)
- **Patterns :** EventBus statique (C# records), Autoloads avec Instance statique, `_Draw()` pour placeholders, `_UnhandledInput()` pour clics monde, UI programmatique
- **Registres :** Static C# dictionaries (PlantRegistry, InsectRegistry) — même pattern que Sprint 4 blueprint

### 8.2 Structure du projet
```
project-flutter/
├── scenes/
│   ├── main/Main.tscn
│   ├── garden/Garden.tscn          # Instancié par zone
│   ├── insects/InsectBase.tscn     # Base + Resources swappables
│   └── ui/                         # HUD, Journal, Shop, ZoneSelector
├── scripts/
│   ├── autoload/
│   │   ├── GameManager.cs          # État global, save/load
│   │   ├── TimeManager.cs          # Cycle jour/nuit
│   │   ├── JournalManager.cs       # Tracking découvertes
│   │   ├── ZoneManager.cs          # État zones, transitions
│   │   ├── PlantLevelManager.cs    # Leveling global par espèce
│   │   └── EventBus.cs             # Pub/sub statique C#
│   ├── data/
│   │   ├── PlantData.cs            # Record avec 13+ champs
│   │   ├── InsectData.cs           # Record avec spawn conditions
│   │   ├── ZoneData.cs             # Grille + état par zone
│   │   └── CellData.cs             # État par cellule
│   ├── registries/
│   │   ├── PlantRegistry.cs        # ~30 plantes, static dict
│   │   └── InsectRegistry.cs       # 72 insectes, static dict
│   └── systems/
│       ├── SpawnSystem.cs           # Logique spawn
│       ├── PhotoSystem.cs           # Qualité photo
│       ├── SprinklerSystem.cs       # Rayon d'arrosage passif
│       └── NectarEconomy.cs         # Gestion monnaie
├── assets/
│   ├── sprites/{plants,insects,tiles,ui,borders}/
│   ├── journal/                     # Illustrations 256×256
│   └── backgrounds/                 # Fonds de zone
├── audio/{music,sfx,ambience}/
└── localization/{en.csv,fr.csv}
```

### 8.3 Architecture multi-zones
- **Toutes les zones en mémoire simultanément** comme siblings Node2D
- Zone active : `Visible = true`, `ProcessMode = Inherit`
- Zones inactives : `Visible = false`, `ProcessMode = Disabled`
- Transition instantanée sans sérialisation ni chargement de scène
- Coût mémoire : <50 KB pour les 7 zones
- Camera bounds ajustées par zone via ZoneManager

### 8.4 Système de sauvegarde
- Fichier JSON
- Sauvegarde : plantes par zone (position, stade), entrées journal (découverts, étoiles), balance nectar, zones débloquées, graines achetées, niveaux globaux de plantes (récoltes par espèce), bordures débloquées, jalons conservation, sprinklers placés, lanterne achetée, temps de jeu, paramètres
- Auto-save toutes les N minutes + sauvegarde à la fermeture
- Slot unique (garder simple)

---

## 9. Sprints de développement

**Basé sur ~20h/semaine (1–2h soirs de semaine + 10–15h week-ends)**

### Sprint 1 — Grille & Plantation (Semaines 1–2, ~40h) ✅ COMPLÉTÉ
- [x] Setup projet Godot, structure de dossiers, autoloads
- [x] Système de grille de jardin (4×4, Node2D grid + Dictionary état)
- [x] Interaction tile (clic pour sélectionner, planter, retirer)
- [x] Scène plante avec 4 stades de croissance (art placeholder `_Draw()`)
- [x] Mécanique d'arrosage
- [x] Cycle jour/nuit basique (CanvasModulate + variable temps) — *mis à jour à 15 min en Sprint 4*
- [x] Bouton d'accélération — *remplacé par horloge analogique cliquable ×0.5/×1/×2 en Sprint 4*
- **Livrable:** Peut placer des plantes, les voir pousser, voir le changement jour/nuit

### Sprint 2 — Insectes & Spawn (Semaines 3–4, ~40h) ✅ COMPLÉTÉ
- [x] Scène insecte de base avec patterns de mouvement (flutter, hover, crawl, erratic)
- [x] Système de spawn : vérifier plantes en floraison → jet pour insectes → spawn dans slots
- [x] Départ d'insecte après durée de visite
- [x] Cap de population par zone
- [x] 3–4 insectes de test avec comportements différents
- [x] Matching insecte-plante basique depuis registres
- **Livrable:** Les insectes arrivent et partent selon ce qui est planté

### Sprint 3 — Photographie & Journal (Semaines 5–6, ~40h) ✅ COMPLÉTÉ
- [x] Toggle mode photo
- [x] Mécanique de focus cercle concentrique (clic & maintien)
- [x] Calcul de qualité (distance du centre)
- [x] Son d'obturateur + effet flash
- [x] UI Journal (grille d'entrées, silhouettes, entrées découvertes)
- [x] Vue détaillée d'entrée journal
- [x] Tracking de découverte (JournalManager autoload)
- [x] Notification nouvelle découverte / fanfare
- **Livrable:** Boucle complète photographier → journal → collection fonctionnelle

### Sprint 4 — Économie, Zones & Temps (Semaines 7–9, ~60h)
- [ ] Système de monnaie nectar (récolter fleurs, gagner nectar)
- [ ] UI Shop de graines (acheter graines avec nectar)
- [ ] Système de déblocage de zones temporaire (coût nectar + seuil journal simple — sera remplacé par babillard au Sprint 5)
- [ ] Construire les 7 zones avec grilles configurables
- [ ] Navigation par onglets entre zones (signets horizontaux)
- [ ] ZoneManager autoload avec transitions de visibilité
- [ ] Tuiles d'eau pour Pond (CellType.Water, animation sine-wave)
- [ ] Bûches et compost pour Deep Wood (3 stades de décomposition)
- [ ] Pierres chauffantes pour Rock Garden
- [ ] Mécanique de serre pour Tropical (brume, lampe UV)
- [ ] Balancement économie : 25 nectar départ → Meadow ~15 min → Tropical ~4h30
- [ ] Hotbar de graines en bas d'écran + curseur fantôme
- [ ] 9+ nouveaux événements EventBus (ZoneChanged, NectarChanged, SeedPurchased, etc.)
- [ ] Sprinklers 3 niveaux (passifs, rayon 3×3 / 5×5 / 7×7)
- [ ] Lanterne de jardin (achat unique, toggle on/off, affecte qualité photo nuit)
- [ ] Système de leveling global des plantes (PlantLevelManager)
- [x] **Refonte cycle jour/nuit : 5 min → 15 min (DAY_CYCLE_DURATION = 900)**
- [x] **Horloge analogique HUD : cadran 12 segments colorés nets, aiguille, chiffre vitesse au centre**
- [x] **Speed control intégré à l'horloge : clic pour cycler ×0.5/×1/×2**
- [x] **Pause monde en mode photo (cycle gelé + timers gelés, insectes bougent)**
- [x] **Pause complète dans journal/shop/menus**
- **Livrable:** Boucle de progression complète du Starter à toutes les zones

### Sprint 5 — Registres de contenu & Babillard (Semaines 10–12, ~50h)
- [ ] PlantRegistry : ~30 plantes avec données complètes (coût, rendement, croissance, attractions, aura)
- [ ] InsectRegistry : 72 espèces avec données complètes (spawn conditions, mouvement, rareté, durée révisée)
- [ ] 3 nouveaux patterns de mouvement (Dart, Skim, Pulse)
- [ ] Conditions de spawn avancées (WaterRequired, MinInsectsPresent, MultiPlantCombo, DecomposingWood, etc.)
- [ ] Spawn weights par rareté (45/25/12/4/1)
- [ ] Pity timer pour espèces rares (garantie après N tentatives)
- [ ] 🧪 Tester soft warning de départ (ailes rapides 5-10s) vs fuite instantanée — garder soft warning pour fin de timer de visite?
- [ ] Son de visiteur rare (chime doux)
- [ ] Slot visiteur spécial pour rare/legendary
- [ ] Texte journal EN + FR pour les 72 espèces
- [ ] Indices de découverte pour toutes les espèces
- [ ] Système de localisation (CSV-based)
- [ ] **Babillard de conservation : section Conservation du journal avec pages par zone**
- [ ] **Slots spécifiques (silhouettes) + slots libres ("?") par zone**
- [ ] **Budget d'étoiles total par zone (compteur ⭐ visible)**
- [ ] **Remplacer le déblocage temporaire Sprint 4 par le système babillard**
- [ ] **Babillard physique dans le Starter Garden (objet décoratif, se remplit au fil du temps)**
- [ ] **Animation de déverrouillage (tampon certification + sentier de fleurs + sparkle tab)**
- [ ] **Mécanique de fuite photo : % par rareté (15/25/40/60/75), bonus aura -15%**
- **Livrable:** Tout le contenu data en jeu, babillard fonctionnel, testable de bout en bout

### Sprint 6 — Art Pipeline (Semaines 12–16, ~100h)
- [ ] Setup pipeline : SAI 128×128 → PNG 32bpp ARGB → Godot
- [ ] Template rigs réutilisables dans Godot (flying, crawling, hovering, swimming)
- [ ] Art insectes prioritaires : 14 Starter + 11 Meadow = **25 espèces** (body parts + journal)
- [ ] Art plantes prioritaires : Starter + Meadow = **12 plantes × 4 stades = 48 sprites**
- [ ] Fonds de zone : Starter + Meadow
- [ ] Tiles de sol basiques
- [ ] Art insectes restants : Forest + Deep Wood + Rock Garden = **29 espèces**
- [ ] Art plantes restantes : Forest → Tropical = **~18 plantes × 4 stades = 72 sprites**
- [ ] Art insectes finaux : Pond + Tropical = **18 espèces** (incluant Legendary showcase)
- [ ] Fonds de zone restants (5)
- [ ] Illustrations journal 256×256 pour les 72 espèces
- [ ] Bordures de photo (8–12 cadres décoratifs)
- [ ] Sprinklers (3 sprites), lanterne (1 sprite), badges feuille (5 icônes)
- [ ] Certificats conservation (4 pages journal)
- [ ] Remplacement de tous les `_Draw()` placeholders par AnimatedSprite2D / Sprite2D
- **Livrable:** Tout l'art en jeu, transition complète des placeholders vers art final

### Sprint 7 — UI & Journal Complet (Semaines 17–18, ~40h)
- [ ] Journal comme hub central avec 5 signets (Insectes, Herbier, Collection, Conservation, Réglages)
- [ ] Animation tourner les pages entre sections
- [ ] UI du shop final (scroll, catégories par zone, prix, aperçus)
- [ ] Section Herbier (niveaux plantes, barres progression, badges)
- [ ] Section Collection (bordures débloquées, statistiques)
- [ ] Section Conservation (certificats, jalons 25/50/75/100%)
- [ ] Réglages dans le journal (son, contrôles, langue)
- [ ] Système photo 3 niveaux visuels (esquisse → aquarelle → vibrante)
- [ ] Application des bordures de photo sur les entrées journal
- [ ] HUD final (compteur nectar animé, horloge analogique colorée, zone actuelle)
- [ ] Zones tabs final (icônes, états verrouillé/déverrouillé, panneau de déblocage)
- [ ] Feedback visuel de récolte (texte flottant, particules, icône volant vers HUD)
- [ ] Juice stack photo (freeze frame, particules, chimes proportionnels à la qualité)
- [ ] Éléments UI artistiques (cadres bois, boutons naturels)
- [ ] Tutoriel hints (tooltips contextuels première fois)
- **Livrable:** Interface complète et polie, journal comme hub central fonctionnel

### Sprint 8 — Audio & Polish (Semaines 19–20, ~40h)
- [ ] Sound design : tous SFX, couches ambiantes, sons d'obturateur (3 couches audio)
- [ ] Musique/audio ambiant : boucle jour, boucle nuit, transitions par zone
- [ ] Sons spécifiques par insecte (buzz, stridulation, scintillement)
- [ ] Son visiteur rare (chime doux)
- [ ] Gonflement ambiant après bonne capture
- [ ] Menu principal (via journal ou écran séparé)
- [ ] Système de sauvegarde/chargement (JSON, auto-save)
- [ ] Récompense journal 100% — certificat platine + événement spécial
- [ ] Tropical Greenhouse : apparition surprise à 75% journal
- [ ] Mécanique firefly nuit (3★ pendant pulse, lanterne OFF)
- [ ] Playtesting et balancement des taux de spawn, économie, leveling plantes
- [ ] Bug fixing
- **Livrable:** Build prêt à tester

### Sprint 9 — Ship (Semaines 21–22, ~40h)
- [ ] Intégration Steam (achievements, assets page store)
- [ ] Trailer de lancement (30–60 secondes, montrer la boucle)
- [ ] Screenshot pour page store (avec art final)
- [ ] Tests de compatibilité (résolutions, performances)
- [ ] Playtesting final par 3–5 personnes
- [ ] Bug fixes finaux
- [ ] Build final et upload Steam
- **Livrable:** LANCEMENT sur Steam

### Objectifs stretch (Post-lancement)
- [ ] Achievements Steam (première photo, zone complète, 100% journal, toutes les 3 étoiles)
- [ ] Photo album feature (sauvegarder photos favorites)
- [ ] Événements saisonniers (migration du Monarque, saison des lucioles)
- [ ] Support manette
- [ ] Bordures de photo additionnelles (DLC gratuit?)

---

## 10. Courbe de progression économique

**Courbe S ciblée pour ~6h de jeu :**

```
Nectar cumulatif
│
│                                            ╭────── Tropical (1000, 75% journal)
│                                       ╭────╯
│                                  ╭────╯        Pond (700)
│                            ╭────╯
│                      ╭────╯              Rock Garden (500)
│                ╭────╯
│          ╭────╯                    Deep Wood (350)
│     ╭────╯
│ ╭──╯                         Forest (200)
│╯                        Meadow (100)
├──────────────────────────────────────────── Temps
0   15m   45m   1h15  2h    3h    4h30  6h+
```

**Revenus estimés par phase :**
- Starter (0–15 min) : ~6–8 plantes, 15–20 nectar/min → accumule 100+
- Starter+Meadow (15–45 min) : ~15 plantes, 25–35 nectar/min → accumule 200+
- 3 zones (45 min–1h15) : ~25 plantes, 40–50 nectar/min → accumule 350+
- 4+ zones (1h15+) : revenus exponentiels avec plantes rares et leveling

**Nectar sinks cumulatifs :**
- Zones : 100+200+350+500+700+1000 = 2850 nectar total
- Sprinklers (si un par zone) : 7 × (40+120+300 max) = potentiellement 1000+ nectar
- Lanterne : 50 nectar
- Graines : variable, ~500–1000 nectar sur une partie complète
- Bordures de photo : ~200–400 nectar (collection complète)

**Principe clé :** L'économie ne devrait jamais être le bottleneck. Le vrai gate est le seuil d'entrées journal — les joueurs doivent explorer et photographier, pas farmer du nectar.

---

## 11. Règles de portée (À COLLER SUR L'ÉCRAN)

1. **~30 plantes. 72 insectes. 7 zones (+1 secrète).** C'est le scope. Respecte-le.
2. **Pas de dialogue. Pas de PNJ actifs. Pas de cutscenes.** Juste jardin, journal, insectes, et texte statique de conservation.
3. **Pas de multijoueur.** Solo uniquement.
4. **Pas de génération procédurale.** Zones fixes, grilles fixes.
5. **Si une feature prend plus de 2 jours, questionne si c'est nécessaire pour v1.0.**
6. **L'art est le bottleneck.** Priorise les systèmes de programmation avec placeholder art, puis remplace avec art final au Sprint 6. Filtre chaque ajout par : "est-ce que ça a besoin de nouvel art majeur?"
7. **Playtest la boucle de base à la fin du Sprint 3.** Si plant → insecte → photo → journal ne feel pas bon, simplifie avant d'ajouter du contenu.
8. **Ship imparfait.** Un jeu fini à 80% bat un jeu inachevé à 100% à chaque fois.
9. **Les Legendary sont du polish, pas du core.** Les 5 Legendary et la Tropical Greenhouse sont les dernières choses à implémenter. Si le temps manque, lance avec 6 zones et 62 espèces.
10. **Le Species Registry v3.0 est canonique.** Toute modification passe par ce document d'abord.
11. **Pas de décorations de jardin** (gnomes, bains d'oiseaux, etc.). Hors scope, trop de nouvel art, sort du but du jeu.
12. **Pas de film caméra.** Caméra illimitée — ne jamais limiter l'action principale du jeu.

---

## 12. Jalons marketing

| Quand | Action |
|-------|--------|
| Semaine 1 | Enregistrer compte Steamworks, payer $100 (timer 30 jours) |
| Semaine 6 | Premier GIF jardin + insecte sur r/IndieDev, r/indiegames |
| Semaine 12 | Page Steam « Coming Soon » live + captures avec art Starter |
| Semaine 14 | GIF montrant mécanique photo + journal illustré (3 niveaux visuels) |
| Semaine 16 | Poster sur subreddits niche : r/gardening, r/insects, r/entomology |
| Semaine 18 | GIF « un insecte Legendary rare apparaît » pour hype |
| Semaine 20 | Envoyer démo/clés à créateurs de contenu cozy |
| Semaine 21 | Trailer de lancement (30–60 secondes, montrer la boucle) |
| Semaine 22 | **LANCEMENT** sur Steam |
| Post-lancement | Soumettre à Wholesome Games, poster sur r/cozygaming |

---

## 13. Variables de configuration

```csharp
// TimeManager.cs
const float DAY_CYCLE_DURATION = 900f;    // 15 minutes (900 secondes) par cycle complet (×1)
const float SEC_PER_GAME_HOUR = 37.5f;    // 900s / 24h = 37.5 secondes réelles par heure jeu
const float SPEED_SLOW = 0.5f;            // ×0.5 → 30 min/jour (contemplatif)
const float SPEED_NORMAL = 1.0f;          // ×1 → 15 min/jour (défaut)
const float SPEED_FAST = 2.0f;            // ×2 → 7.5 min/jour (accéléré)

// Périodes du jour (en heures jeu, pour couleur horloge + spawns)
const float DAWN_START = 5f;              // 5h → rose-orangé
const float MORNING_START = 7f;           // 7h → jaune doré
const float AFTERNOON_START = 12f;        // 12h → bleu ciel
const float DUSK_START = 17f;             // 17h → violet-ambre
const float NIGHT_START = 19f;            // 19h → indigo/bleu nuit
// Nuit dure de 19h à 5h = 10h jeu = ~6m15s réelles (42% du cycle)

// Pause comportement
// Mode photo: cycle gelé + timers visite gelés, insectes BOUGENT encore
// Journal/Shop/Menus: pause complète (monde + insectes)

// SpawnSystem.cs
const float SPAWN_CHECK_INTERVAL = 5f;    // secondes entre tentatives
const int MAX_INSECTS_PER_ZONE = 12;      // cap population par zone
const float DESPAWN_CHECK_INTERVAL = 10f; // secondes entre vérifications départ

// Spawn weights par rareté
const int WEIGHT_COMMON = 45;
const int WEIGHT_UNCOMMON = 25;
const int WEIGHT_RARE = 12;
const int WEIGHT_VERY_RARE = 4;
const int WEIGHT_LEGENDARY = 1;

// Durées de visite (secondes)
const float VISIT_COMMON_MIN = 120f;      // 2 min
const float VISIT_COMMON_MAX = 300f;      // 5 min
const float VISIT_UNCOMMON_MIN = 60f;     // 1 min
const float VISIT_UNCOMMON_MAX = 180f;    // 3 min
const float VISIT_RARE_MIN = 45f;
const float VISIT_RARE_MAX = 120f;        // 2 min
const float VISIT_VERY_RARE_MIN = 30f;
const float VISIT_VERY_RARE_MAX = 90f;    // 1.5 min
const float VISIT_LEGENDARY_MIN = 30f;
const float VISIT_LEGENDARY_MAX = 60f;    // 1 min

// NectarEconomy.cs
const int STARTING_NECTAR = 25;
const int HARVEST_NECTAR_COMMON = 3;
const int HARVEST_NECTAR_UNCOMMON = 5;
const int HARVEST_NECTAR_RARE = 8;
const float REGROW_RATIO = 0.5f;          // regrow = 50% du temps de croissance initial

// PhotoSystem.cs
const float FOCUS_DURATION = 1.5f;        // secondes pour fermer le cercle
const float THREE_STAR_RADIUS = 0.15f;    // % du rayon pour shot parfait
const float TWO_STAR_RADIUS = 0.40f;      // % pour 2 étoiles
const float FREEZE_DURATION = 0.5f;       // 500ms freeze frame uniforme (toute qualité)

// PhotoFleeSystem.cs
const float FLEE_CHANCE_COMMON = 0.15f;      // 15% → ~6-7 essais moyens
const float FLEE_CHANCE_UNCOMMON = 0.25f;    // 25% → ~4 essais
const float FLEE_CHANCE_RARE = 0.40f;        // 40% → ~2-3 essais
const float FLEE_CHANCE_VERY_RARE = 0.60f;   // 60% → ~1-2 essais
const float FLEE_CHANCE_LEGENDARY = 0.75f;   // 75% → ~1.3 essais
const float AURA_FLEE_REDUCTION = 0.15f;     // -15% si plante level 3+, pas de plancher
// Un 3★ réussi ne déclenche JAMAIS de jet de fuite

// ConservationBoard.cs (babillard)
// Photos requises et étoiles par zone — voir §4.10 pour le tableau complet
// Meadow: 5 photos (2 spéc + 3 libres), 8⭐ total
// Pond:   8 photos (3 spéc + 5 libres), 20⭐ total

// PlantLevelManager.cs
const int LEVEL_2_HARVESTS = 5;
const int LEVEL_3_HARVESTS = 15;
const int LEVEL_4_HARVESTS = 30;
const int LEVEL_5_HARVESTS = 50;

// SprinklerSystem.cs
const int SPRINKLER_1_RADIUS = 1;         // 3×3
const int SPRINKLER_2_RADIUS = 2;         // 5×5
const int SPRINKLER_3_RADIUS = 3;         // 7×7
const int SPRINKLER_1_COST = 40;
const int SPRINKLER_2_COST = 120;
const int SPRINKLER_3_COST = 300;

// LanternSystem.cs
const int LANTERN_COST = 50;

// ConservationMilestones.cs
const float MILESTONE_BRONZE = 0.25f;     // 25% = 18/72
const float MILESTONE_SILVER = 0.50f;     // 50% = 36/72
const float MILESTONE_GOLD = 0.75f;       // 75% = 54/72 → Tropical unlock
const float MILESTONE_PLATINUM = 1.00f;   // 100% = 72/72 → récompense finale
```

---

## 14. Notes techniques — TODO différé

### Tile size (actuellement 128px, cible 64px)
Le GDD spécifie des tiles de 64×64px (§4.1), mais le prototype utilise 128px. Ce changement sera fait lors du **Sprint 6 (Art Pipeline)** quand les sprites finaux remplaceront les placeholders `_Draw()`. Pas d'impact fonctionnel tant qu'on est en placeholder.

### Items de debug à retirer avant ship
Ces fonctionnalités de debug sont utiles pendant le développement mais **doivent être retirées ou désactivées** avant le build final (Sprint 9) :

| Item | Fichier | Description |
|------|---------|-------------|
| F1 — Unlock all zones | `HUD.cs` + `ZoneManager.cs` | Débloque toutes les zones sans coût nectar ni prérequis journal |
| F2 — Spawn debug bee | `HUD.cs` | Spawne une Honeybee statique au centre de la caméra pour tester la photo |
| F3 — Toggle ×10 speed | `HUD.cs` + `TimeManager.cs` | Toggle vitesse ×10 debug, reclique restaure la vitesse précédente (×0.5, ×1 ou ×2) |
| F4 — Toggle ×50 speed | `HUD.cs` + `TimeManager.cs` | Toggle vitesse ×50 debug, reclique restaure la vitesse précédente (×0.5, ×1 ou ×2) |
| F5 — Fill journal 53 | `HUD.cs` + `JournalManager.cs` | Remplit le journal avec 53 espèces (2★) pour tester l'unlock Tropical à 75% (54 requis) |
| Debug hint label | `HUD.tscn` | Label raccourcis debug en bas à gauche (40% opacité) |

---

## 15. Changements depuis GDD v2.0

| Aspect | v2.0 | v3.0 | Raison |
|--------|------|------|--------|
| Durées insectes | 2–15s (action-game) | 30s–5min (cozy) | Recherche genre cozy : récompenser observation, pas réflexes |
| Photo qualité | 3 étoiles simple | 3 étoiles → 3 niveaux visuels journal (esquisse/aquarelle/vibrante) | Motivation complétionniste, zéro art supplémentaire |
| Photographie nuit | Non spécifié | Max 2★ sans lanterne, 3★ avec. Exception firefly (3★ pendant pulse, lanterne OFF) | Progression significative, micro-décisions satisfaisantes |
| Lanterne de jardin | Non existant | Achat unique ~50 nectar, toggle on/off | Sink utile, upgrade de confort |
| Sprinklers | Non existant | 3 niveaux passifs (3×3, 5×5, 7×7) | QoL progression, sink significatif, automatisation comme récompense |
| Plant leveling | Non existant | Global par espèce, 5 niveaux (5/15/30/50 récoltes), aura en floraison | Profondeur sur core loop sans bloat |
| Badges plante | Non existant | 5 couleurs feuille (brun→vert→bleu→violet→doré) | Feedback visuel de progression |
| Bordures photo | Non existant | 8–12 cadres décoratifs (15–50 nectar) | Sink cosmétique infini, personnalisation |
| Narrative | "Pas d'histoire" | Agence de conservation, jalons 25/50/75/100% | Raison de remplir le journal, récompenses de progression |
| Tropical Greenhouse | Zone 7 visible | Zone secrète cachée jusqu'à 75% journal | Moment surprise et délice |
| Journal | Simple collectathon | Hub central avec 5 onglets (Insectes, Herbier, Collection, Conservation, Réglages) | Interface immersive unique, pas de menu séparé |
| Arrosage | Manuel seulement | Manuel + sprinklers passifs | Automatisation comme progression |
| Plantes sans eau | Ralentissent | Arrêtent complètement de pousser (mais ne meurent jamais) | Plus cozy, pression douce |
| Indices de départ | Disparition abrupte | Soft warning (ailes rapides 5–10s) — 🧪 **à tester** : fuite instantanée post-photo vs soft warning | Design cozy vs tension Pokémon — tester les deux |
| Slot visiteur spécial | Non existant | 1 slot dédié rare/legendary | Les rares ne compétitionnent pas avec les communs |
| Son rare | Non existant | Chime doux à l'arrivée rare/legendary | Alerte sans alarme |
| Juice photo | Flash simple | Stack proportionnel (freeze frame, particules, chimes, bloom) | Le shutter doit être le meilleur feeling du jeu |
| Scope rules | 10 règles | 12 règles (+pas de décorations, +caméra illimitée) | Filtres anti-scope-creep |
| Sauvegarde | Basique | + niveaux plantes, bordures, jalons conservation, sprinklers, lanterne, babillard | Nouveaux systèmes à persister |
| Cycle jour/nuit | 5 min par cycle | **15 min par cycle** (×1), ajustable ×0.5/×1/×2 | Recherche 20+ jeux : médiane genre = 15–20 min. 5 min était 3× plus rapide que Graveyard Keeper |
| Vitesse de jeu | ×1, ×2, ×3 (bouton séparé) | **×0.5, ×1, ×2** (clic sur horloge, chiffre blanc au centre) | Les joueurs veulent ralentir, pas accélérer (données Coral Island, Sandrock, mods Stardew) |
| Affichage temps | Aucun affichage dédié | **Horloge analogique HUD** : 12 segments colorés nets par période, ~128px, style bois/laiton | Information utile + esthétique nature, pas de chiffre stressant |
| Pause en photo | Non spécifié | **Cycle gelé + timers gelés, insectes bougent** | Ne jamais pénaliser l'activité principale ; skill de tracking préservé (modèle Pokémon Snap) |
| Pause menus | Non spécifié | **Pause complète** (journal, shop, réglages) | Standard du genre, anti-stress |
| Fuite photo | Non existant | **% de fuite par rareté** (15/25/40/60/75%) après essai non-3★. Aura plante level 3+ = -15%. | Empêcher le 3★ trivial ; inspiration poisson légendaire Stardew, fuite Pokémon |
| Déblocage zones | Bouton "acheter" (nectar + seuil journal) | **Babillard de conservation** : photos spécifiques + libres + budget ⭐ + nectar | Inspiration bundles Stardew Valley — action tangible, progression visuelle, moment mémorable |
| Sprint 4 scope | Économie + Zones + tout | Sprint 4 = core (économie, zones, temps). **Babillard + fuite → Sprint 5** | Découpage scope : babillard dépend des 72 espèces définies |

---

*Ce document est la source unique de vérité pour Project Flutter. En cas de doute, consulter ici. Quand le scope creep menace, relire la Section 11. Le Species Registry v3.0 est la référence canonique pour les espèces.*
