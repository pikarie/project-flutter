# Project Flutter — Game Design Document
**Version:** 2.0  
**Date:** 2026-02-14  
**Moteur:** Godot 4.5 (C# 12 / .NET 8)  
**Développeuse:** Karianne (solo)  
**Plateformes:** Windows (Steam), potentiel Linux/Mac  
**Prix cible:** $7.99–$9.99  
**Durée de jeu:** 6–8 heures pour compléter le journal (100%)  
**Langues:** English + Français (UI + texte journal, aucun dialogue)

---

## 1. Vision

**Pitch en une phrase:** Cultive un jardin peint à la main pour attirer et photographier 72 espèces d'insectes dans un sim nature cozy sans pression.

**Pitch d'ascenseur:** Project Flutter est un jeu de jardinage top-down où tu plantes des fleurs et des herbes pour attirer de vrais insectes — abeilles, papillons, papillons de nuit, libellules, lucioles. Chaque plante attire des espèces spécifiques. Tu photographies les insectes pour les documenter dans un journal de terrain illustré à la main. Un cycle jour/nuit transforme ton jardin : abeilles et papillons le jour, papillons de nuit et lucioles la nuit. Équilibre la récolte de nectar pour ta monnaie et le maintien des fleurs en floraison pour attirer les espèces rares. Commence par un petit jardin et débloque des prairies, forêts, étangs et une serre tropicale. Pas de dialogue, pas d'histoire, pas d'échec — juste le plaisir de cultiver, découvrir et compléter ta collection.

**Fantaisie fondamentale:** « J'ai créé ce beau jardin, et regarde qui est venu visiter. »

**Jeux comparables:**
- Neko Atsume (poser objets → créatures arrivent → les collectionner)
- APICO / Mudborne (journal nature, découverte par expérimentation)
- Stardew Valley (style visuel de référence, vue top-down légèrement inclinée)
- Viridi (entretien de plantes, croissance en temps réel)

---

## 2. Boucle de jeu

```
PLANTER graines → CULTIVER & ENTRETENIR → ATTIRER insectes →
PHOTOGRAPHIER pour documenter → GAGNER nectar → ACHETER graines/zones → RÉPÉTER
```

**Déroulement de session (20–45 min):**
1. Vérifier le jardin — voir ce qui fleurit, quels insectes sont arrivés
2. Photographier les nouveaux insectes / reprendre ceux non documentés
3. Récolter du nectar sur certaines fleurs (les rend temporairement moins attractives)
4. Acheter de nouvelles graines avec le nectar gagné
5. Attendre ou accélérer la croissance et les nouvelles arrivées
6. La nuit tombe — nouveaux insectes, nouvelles fleurs nocturnes
7. Photographier les espèces nocturnes
8. Changer de zone pour explorer d'autres habitats
9. Fin de session ou continuer

---

## 3. Style artistique

- **Illustration numérique peinte à la main** (Paint Tool SAI, tablette graphique)
- **PAS du pixel art** — style illustré lisse et chaleureux
- **Vue top-down avec léger angle** (perspective Stardew Valley, ~30° d'inclinaison)
- **Palette de couleurs:** Tons chauds et naturels. Verts, bruns terreux, couleurs de fleurs pop contre le sol
- **Résolution:** Tiles de 64×64px, sprites peints à 128×128 (2× display) pour rendu net avec filtrage linéaire
- **Sprites d'insectes:** 32–48px en jeu, peints à 128×128. Animation par tweening de parties du corps (ailes, pattes, antennes) via AnimationPlayer
- **Illustrations journal:** 256×256, détaillées — c'est la « récompense » artistique
- **Pipeline art:** Peindre les parties du corps en SAI → exporter en PNG 32bpp ARGB → assembler les animations dans Godot avec tweens → shader de vent pour les plantes
- **UI:** Propre et minimaliste. Esthétique cadre bois/naturel. Icônes plutôt que texte

---

## 4. Mécaniques de jeu

### 4.1 Grille de jardin
- Système de placement **snap-to-grid** avec tiles carrées (64×64px)
- Chaque tile contient : une plante, un objet d'infrastructure (eau, pierre, bûche), ou vide (sol/herbe)
- Plantes occupent 1×1 tile (exception : quelques plantes 2×2 exotiques)
- **Aucun personnage joueur** — interaction directe avec le curseur (clic pour planter, cliquer pour récolter)
- Curseurs personnalisés (petite truelle pour planter, appareil photo pour photographier)

### 4.2 Croissance des plantes
- Plantes poussent en **4 stades visuels:** Graine → Pousse → Croissance → Floraison
- Seules les plantes en **Floraison** attirent des insectes
- Vitesse de croissance par rareté :
  - Common : ~2 cycles (60s)
  - Uncommon : ~3 cycles (90s)
  - Rare : ~4–5 cycles (120–150s)
- Les plantes ne meurent **jamais** de négligence (design cozy, sans pression)
- Les plantes non arrosées pausent simplement leur croissance
- **Après récolte**, les plantes reviennent au stade Croissance et doivent reflorir :
  - Common : 1 cycle (30s) — récolte libre
  - Uncommon : 1.5 cycles (45s) — pause gérable
  - Rare : 2 cycles (60s) — planifier autour
- Animation de transition : shrink 80% → swap texture → bounce 110% → settle 100% + particules feuilles
- Shader de vent (vertex shader sine-based) pour animation de balancement, pas de frames supplémentaires

### 4.3 Système de spawn d'insectes
- Chaque plante en floraison a un nombre limité de **slots d'insectes** (1–3 selon plante)
- Toutes les quelques secondes, le système vérifie chaque plante en floraison et peut spawner un insecte si :
  - Il y a un slot libre
  - Les conditions de l'insecte sont remplies (heure, plantes requises, zone, mécaniques spéciales)
  - Le jet de dé passe le poids de rareté de l'insecte
- **Poids de spawn par rareté:** Common 45, Uncommon 25, Rare 12, Very Rare 4, Legendary 1
- Les insectes **arrivent graduellement** et **partent après une durée définie** :
  - Common : 8–15s (plus de temps pour photographier)
  - Uncommon : 6–10s
  - Rare : 4–7s
  - Very Rare : 3–5s
  - Legendary : 2–4s (fenêtre très courte!)
- **Cap de population** par zone (8–15 insectes visibles selon taille de zone)
- Conditions de spawn spéciales :
  - `PlantAttraction` : l'insecte apparaît près de plantes spécifiques
  - `WaterRequired` : nécessite des tuiles d'eau dans la zone
  - `MinInsectsPresent` : apparaît quand N+ insectes sont déjà actifs (prédateurs)
  - `MultiPlantCombo` : nécessite plusieurs plantes spécifiques en floraison
  - `MinPlantDiversity` : nécessite N+ espèces de plantes différentes
  - `DecomposingWood` : nécessite des bûches à un certain stade de décomposition
  - `SunTrapRock` : nécessite des pierres chauffées par le soleil
  - `UVLamp` : nécessite une lampe UV placée (pour papillons de nuit)

### 4.4 Mécanique de photographie
- Basculer en **mode photo** (toggle ou maintenir une touche)
- **Cliquer et maintenir** sur un insecte pour commencer la mise au point
- Un **cercle concentrique se ferme** autour de l'insecte (1–2 secondes)
- L'insecte **continue de bouger** pendant la mise au point
- Quand le cercle se ferme complètement : **son d'obturateur + flash blanc bref + insecte freeze momentanément**
- **Qualité photo** basée sur le centrage de l'insecte :
  - ★☆☆ — insecte près du bord (documenté, entrée basique)
  - ★★☆ — insecte raisonnablement centré
  - ★★★ — centrage parfait (débloque détail bonus, fun fact, ou illustration alternative)
- Première photo réussie d'une espèce = **nouvelle entrée journal** (récompense principale)
- Peut rephotographier pour améliorer le classement étoiles
- **Patterns de comportement affectent la difficulté** selon les 7 types de mouvement : Hover, Flutter, Crawl, Erratic, Dart, Skim, Pulse

### 4.5 Journal de terrain
- Interface collectathon centrale accessible via icône de livre
- **Grille d'entrées** : espèces découvertes montrent des portraits illustrés ; non découvertes montrent des silhouettes grises
- Chaque entrée contient :
  - Nom d'espèce (EN/FR)
  - Illustration peinte à la main (grande, détaillée)
  - Classement étoiles de la meilleure photo (★☆☆ à ★★★)
  - Texte de saveur / fun fact du monde réel
  - Indice d'habitat (« Trouvé près de la lavande pendant la journée »)
  - Date de première découverte
  - Zone d'origine et période d'activité
- **Indices de découverte** pour espèces non découvertes : indices vagues apparaissent aux jalons du journal
- **Compteur de complétion** : « 37/72 espèces documentées » avec pourcentage
- **Compléter le journal 100%** = récompense finale (effet visuel spécial, insecte légendaire secret, scène de crédits)
- Organisation par zone avec onglets, filtrage par catégorie (papillons, coléoptères, etc.)

### 4.6 Monnaie & Économie
- **Monnaie : Nectar** (monnaie universelle)
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

### 4.7 Cycle jour/nuit
- **Durée configurable** (défaut : ~5 minutes réelles par cycle complet, ajustable via `DAY_CYCLE_DURATION`)
- **Bouton d'accélération** (×1, ×2, ×3) pour les périodes d'attente
- **Transition visuelle :** CanvasModulate avec changement graduel de couleur, éclairage, ombres
- **Impact gameplay :**
  - **Insectes diurnes (51):** abeilles, papillons, coccinelles, libellules
  - **Insectes nocturnes (16):** papillons de nuit, lucioles, grillons
  - **Insectes crépusculaires (5):** chrysope, lucane, sphinx de la vigne
  - **Plantes nocturnes** s'ouvrent seulement la nuit (onagre, etc.)
  - Les plantes diurnes se ferment la nuit (mais ne perdent pas de progrès)
- **Audio ambiant :** chant d'oiseaux → grillons/grenouilles → chorus de l'aube

### 4.8 Progression par zones

**7 zones avec déverrouillage progressif (double-gate : nectar dépensé + entrées journal atteintes):**

| # | Zone | Grille | Coût nectar | Entrées requises | Temps estimé | Espèces |
|---|------|--------|-------------|------------------|--------------|---------|
| 1 | Starter Garden 🌱 | 5×5 | Gratuit | 0 | 0 min | 14 |
| 2 | Meadow 🌻 | 6×6 | 100 | 5 | ~15 min | 11 |
| 3 | Forest 🌲 | 6×6 | 200 | 15 | ~45 min | 11 |
| 4 | Deep Wood 🪵 | 5×5 | 350 | 25 | ~1h15 | 9 |
| 5 | Rock Garden ⛰️ | 5×5 | 500 | 35 | ~2h | 9 |
| 6 | Pond 🐸 | 5×5 | 700 | 45 | ~3h | 8 |
| 7 | Tropical Greenhouse 🌺 | 7×7 | 1000 | 55 | ~4h30 | 10 |

Le joueur peut naviguer librement entre les zones débloquées. Chaque zone a un art de fond unique, des sons ambiants distincts, et des mécaniques spéciales.

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
- Autres : Pillbug (Common, se roule en boule — isopode*), Ant (Common, piste en file)
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

### Zone 7 — Tropical Greenhouse 🌺
*Serre vitrée, orchidées, vignes, brume — zone endgame*

**10 espèces** (0 Common, 0 Uncommon, 2 Rare, 4 Very Rare, 4 Legendary) | 4–5 plantes exotiques
- Papillons : Zebra Longwing (Rare), Ulysses Butterfly (Very Rare), Queen Alexandra's Birdwing (Legendary, plus grand papillon au monde)
- P. de nuit : Atlas Moth (Very Rare), Comet Moth (Legendary, queues de 15 cm), Madagascan Sunset Moth (Very Rare)
- Abeilles : Orchid Bee (Very Rare), Blue-Banded Bee (Rare)
- Coléoptères : Hercules Beetle (Legendary, nuit, énormes pinces)
- Autres : Orchid Mantis (Legendary, pattes en forme de pétales)
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
| Fonds de zone (7) | 7 | Plein écran | Starter, Meadow, Forest, Deep Wood, Rock, Pond, Tropical |
| Tile sprites (sol, herbe, eau, pierre, bois) | ~25 | 64×64 | Réutilisables selon zones |
| Éléments UI (boutons, cadres, icônes) | ~30 | Divers | Journal, shop, HUD, tabs zone |
| Art de curseur (2 modes) | 2 | ~32×32 | Truelle, appareil photo |
| Animation cercle photo | 1 | Programmatique | Cercle concentrique + flash |
| **Total assets uniques** | **~549** | | |

**Estimation de production art :** 150–250 heures au total
- Insectes (sprites + journal) : 80–120h
- Plantes (120 stades) : 40–60h
- Fonds + tiles + UI : 30–70h

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
- **Photographie :** clic mécanique d'obturateur, ronronnement de mise au point, son d'avancement de film
- **Insectes :** sons ambiants spécifiques par espèce (buzz d'abeille, chant de grillon, scintillement de luciole, stridulation de cigale)
- **UI :** clics de bois doux pour boutons, tournement de page pour journal, carillon doux pour nouvelle découverte
- **Fanfare de découverte :** jingle spécial quand une nouvelle espèce est documentée

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
│   │   └── EventBus.cs             # Pub/sub statique C#
│   ├── data/
│   │   ├── PlantData.cs            # Record avec 13+ champs
│   │   ├── InsectData.cs           # Record avec spawn conditions
│   │   ├── ZoneData.cs             # Grille + état par zone
│   │   └── CellData.cs             # État par cellule
│   ├── registries/
│   │   ├── PlantRegistry.cs        # 30 plantes, static dict
│   │   └── InsectRegistry.cs       # 72 insectes, static dict
│   └── systems/
│       ├── SpawnSystem.cs           # Logique spawn
│       ├── PhotoSystem.cs           # Qualité photo
│       └── NectarEconomy.cs         # Gestion monnaie
├── assets/
│   ├── sprites/{plants,insects,tiles,ui}/
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
- Sauvegarde : plantes par zone (position, stade), entrées journal (découverts, étoiles), balance nectar, zones débloquées, graines achetées, temps de jeu, paramètres
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
- [x] Cycle jour/nuit basique (CanvasModulate + variable temps)
- [x] Bouton d'accélération (×1, ×2, ×3)
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

### Sprint 4 — Économie & Zones (Semaines 7–9, ~60h)
- [ ] Système de monnaie nectar (récolter fleurs, gagner nectar)
- [ ] UI Shop de graines (acheter graines avec nectar)
- [ ] Système de déblocage de zones (coût nectar + seuil journal)
- [ ] Construire les 7 zones avec grilles configurables
- [ ] Navigation par onglets entre zones
- [ ] ZoneManager autoload avec transitions de visibilité
- [ ] Tuiles d'eau pour Pond (CellType.Water, animation sine-wave)
- [ ] Bûches et compost pour Deep Wood (3 stades de décomposition)
- [ ] Pierres chauffantes pour Rock Garden
- [ ] Mécanique de serre pour Tropical (brume, lampe UV)
- [ ] Balancement économie : 25 nectar départ → Meadow ~15 min → Tropical ~4h30
- [ ] Hotbar de graines en bas d'écran + curseur fantôme
- [ ] 9 nouveaux événements EventBus (ZoneChanged, NectarChanged, SeedPurchased, etc.)
- **Livrable:** Boucle de progression complète du Starter à toutes les zones

### Sprint 5 — Registres de contenu (Semaines 10–11, ~40h)
- [ ] PlantRegistry : 30 plantes avec données complètes (coût, rendement, croissance, attractions)
- [ ] InsectRegistry : 72 espèces avec données complètes (spawn conditions, mouvement, rareté, durée)
- [ ] 3 nouveaux patterns de mouvement (Dart, Skim, Pulse)
- [ ] Conditions de spawn avancées (WaterRequired, MinInsectsPresent, MultiPlantCombo, DecomposingWood, etc.)
- [ ] Spawn weights par rareté (45/25/12/4/1)
- [ ] Pity timer pour espèces rares (garantie après N tentatives)
- [ ] Texte journal EN + FR pour les 72 espèces
- [ ] Indices de découverte pour toutes les espèces
- [ ] Système de localisation (CSV-based)
- **Livrable:** Tout le contenu data en jeu, testable de bout en bout

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
- [ ] Remplacement de tous les `_Draw()` placeholders par AnimatedSprite2D / Sprite2D
- **Livrable:** Tout l'art en jeu, transition complète des placeholders vers art final

### Sprint 7 — UI & Polissage (Semaines 17–18, ~40h)
- [ ] UI du shop final (scroll, catégories par zone, prix, aperçus)
- [ ] UI du journal final (onglets par zone, filtres par catégorie, animations de page)
- [ ] HUD final (compteur nectar animé, indicateur jour/nuit, zone actuelle)
- [ ] Zones tabs final (icônes, états verrouillé/déverrouillé, panneau de déblocage)
- [ ] Feedback visuel de récolte (texte flottant, particules, icône volant vers HUD)
- [ ] Éléments UI artistiques (cadres bois, boutons naturels)
- [ ] Tutoriel hints (tooltips contextuels première fois)
- **Livrable:** Interface complète et polie

### Sprint 8 — Audio & Polish (Semaines 19–20, ~40h)
- [ ] Sound design : tous SFX, couches ambiantes, sons d'obturateur
- [ ] Musique/audio ambiant : boucle jour, boucle nuit, transitions par zone
- [ ] Sons spécifiques par insecte (buzz, stridulation, scintillement)
- [ ] Menu principal, écran de paramètres (volume, langue, plein écran)
- [ ] Système de sauvegarde/chargement
- [ ] Récompense journal 100% (événement spécial)
- [ ] Playtesting et balancement des taux de spawn et de l'économie
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
- [ ] Système cadeau/souvenir (insectes laissent des objets collectibles)
- [ ] Événements saisonniers (migration du Monarque, saison des lucioles)
- [ ] Support manette
- [ ] Mode nuit avancé avec lampe UV interactive

---

## 10. Courbe de progression économique

**Courbe S ciblée pour ~6h de jeu :**

```
Nectar cumulatif
│
│                                            ╭────── Tropical (1000)
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
- 4+ zones (1h15+) : revenus exponentiels avec plantes rares

**Principe clé :** L'économie ne devrait jamais être le bottleneck. Le vrai gate est le seuil d'entrées journal — les joueurs doivent explorer et photographier, pas farmer du nectar.

---

## 11. Règles de portée (À COLLER SUR L'ÉCRAN)

1. **~30 plantes. 72 insectes. 7 zones.** C'est le scope. Respecte-le.
2. **Pas de dialogue. Pas d'histoire. Pas de PNJ.** Juste jardin, journal et insectes.
3. **Pas de multijoueur.** Solo uniquement.
4. **Pas de génération procédurale.** Zones fixes, grilles fixes.
5. **Si une feature prend plus de 2 jours, questionne si c'est nécessaire pour v1.0.**
6. **L'art est le bottleneck.** Priorise les systèmes de programmation avec placeholder art, puis remplace avec art final au Sprint 6.
7. **Playtest la boucle de base à la fin du Sprint 3.** Si plant → insecte → photo → journal ne feel pas bon, simplifie avant d'ajouter du contenu.
8. **Ship imparfait.** Un jeu fini à 80% bat un jeu inachevé à 100% à chaque fois.
9. **Les Legendary sont du polish, pas du core.** Les 5 Legendary et la Tropical Greenhouse sont les dernières choses à implémenter. Si le temps manque, lance avec 6 zones et 62 espèces.
10. **Le Species Registry v3.0 est canonique.** Toute modification passe par ce document d'abord.

---

## 12. Jalons marketing

| Quand | Action |
|-------|--------|
| Semaine 1 | Enregistrer compte Steamworks, payer $100 (timer 30 jours) |
| Semaine 6 | Premier GIF jardin + insecte sur r/IndieDev, r/indiegames |
| Semaine 12 | Page Steam « Coming Soon » live + captures avec art Starter |
| Semaine 14 | GIF montrant mécanique photo + journal illustré |
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
const float DAY_CYCLE_DURATION = 300f;    // secondes par cycle complet (5 min défaut)
const float DAWN_RATIO = 0.05f;           // % du cycle = aube
const float DUSK_RATIO = 0.05f;           // % du cycle = crépuscule

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
```

---

## 14. Changements depuis GDD v1.0

| Aspect | v1.0 | v2.0 | Raison |
|--------|------|------|--------|
| Zones | 4 (Starter, Meadow, Pond, Night) | 7 (+ Forest, Deep Wood, Rock Garden, Tropical) | Contenu plus riche, progression plus longue |
| Insectes | 25 | 72 | Comparaison marché (APICO 45+, Fields of Mistria 68) |
| Plantes | 20 | ~30 | Support pour 72 insectes + mécaniques non-plantes |
| Langage | GDScript | C# 12 / .NET 8 | Décision technique post-GDD v1 |
| Playtime | 3–6h | 6–8h | Plus de contenu à explorer |
| Prix | $4.99–$6.99 | $7.99–$9.99 | Justifié par contenu 3× plus large |
| Sprints | 6 (12 semaines) | 9 (22 semaines) | Art pipeline est le nouveau bottleneck |
| Starter grille | 4×4 | 5×5 | Plus d'espace pour tutoriel |
| Rareté | 4 tiers | 5 tiers (+Legendary) | Endgame spectaculaire |
| Animation | Non spécifié | Tweened body parts + shader vent | Recherche pipeline art Sprint 5 |
| Mécaniques spéciales | Aucune | Bûches, compost, pierres, UV, pistes de fourmis | Variété par zone |
| Night Garden | Zone dédiée | Intégrée dans toutes les zones (16 nocturnes répartis) | Plus naturel |

---

*Ce document est la source unique de vérité pour Project Flutter. En cas de doute, consulter ici. Quand le scope creep menace, relire la Section 11. Le Species Registry v3.0 est la référence canonique pour les espèces.*
