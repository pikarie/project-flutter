# Project Flutter — Species Registry v3.0 (FINAL)
## 72 Espèces × 7 Zones × ~30 Plantes

**Version finale après curation. Ce document est la référence canonique pour toutes les décisions de sprint.**

---

## Changements depuis v2.0

| Action | Espèce | Zone affectée |
|--------|--------|---------------|
| ❌ Coupé | 15-Spot Ladybug | Starter (doublon visuel de la 7-Spot) |
| ❌ Coupé | Comma | Rock Garden (doublon visuel du Question Mark) |
| ❌ Coupé | Black Weevil (Baris) | Deep Wood (doublon du Myllocerus) |
| ❌ Coupé | Bee Fly | Meadow (rôle couvert par Marmalade Hoverfly) |
| ❌ Coupé | Wandering Glider | Rock Garden (libellule sans eau = incohérent) |
| ✅ Ajouté | Field Cricket | Rock Garden (Common nocturne, mécanique sonore) |
| ✅ Ajouté | Pillbug | Rock Garden (Common, mécanique « se roule en boule ») |
| ✅ Ajouté | Ant | Rock Garden (Common, mécanique de piste en file) |

**Résultat : 72 espèces, 7 zones, distribution de rareté ≈ cible**

---

## Distribution de rareté — finale

| Rareté | # | % réel | Cible (%) | Statut |
|--------|---|--------|-----------|--------|
| Common | 27 | 37.5% | 40% | ≈ (légèrement sous — acceptable) |
| Uncommon | 22 | 30.6% | 30% | ✅ |
| Rare | 11 | 15.3% | 15% | ✅ |
| Very Rare | 7 | 9.7% | 10% | ✅ |
| Legendary | 5 | 6.9% | 5% | ≈ (légèrement au-dessus — voulu, endgame riche) |
| **Total** | **72** | **100%** | | |

---

## Zones — Vue détaillée

### Zone 1 — Starter Garden 🌱
*Cottage garden, plates-bandes fleuries, palissade — zone tutoriel*
**14 espèces** | Disponible dès le début | ~5-6 plantes

| # | Espèce | FR | Catégorie | Rareté | Actif | Couleur dominante |
|---|--------|----|-----------|--------|-------|-------------------|
| 1 | Cabbage White | Piéride du chou | Papillon | Common | Jour | Blanc, points noirs |
| 2 | Orange Tip | Aurore | Papillon | Common | Jour | Blanc + pointes orange (mâle) |
| 3 | Red Admiral | Vulcain | Papillon | Uncommon | Jour | Noir velouté, bandes rouge-orange |
| 4 | Rosy Maple Moth | Anisote de l'érable | P. de nuit | Common | Nuit | Rose + jaune pastel |
| 5 | Garden Tiger Moth | Écaille martre | P. de nuit | Common | Nuit | Brun-blanc / orange+bleu caché |
| 6 | Seven-Spot Ladybug | Coccinelle à 7 points | Coléoptère | Common | Jour | Rouge, 7 points noirs |
| 7 | 22-Spot Ladybug | Coccinelle à 22 points | Coléoptère | Common | Jour | **Jaune**, points noirs |
| 8 | Golden Tortoise Beetle | Casside dorée | Coléoptère | Uncommon | Jour | Or métallique → rouge quand dérangé |
| 9 | Rose Chafer | Cétoine dorée | Coléoptère | Common | Jour | Vert-or iridescent |
| 10 | Japanese Beetle | Scarabée japonais | Coléoptère | Common | Jour | Vert métallique + bronze cuivré |
| 11 | Marmalade Hoverfly | Syrphe ceinturé | Syrphe | Common | Jour | Orange, bandes noires |
| 12 | Green Lacewing | Chrysope verte | Autre | Common | Crépuscule | Vert pâle, ailes dentelle |
| 13 | European Mantis | Mante religieuse | Autre | Uncommon | Jour | Vert vif, pose « prière » |
| 14 | Western Honeybee | Abeille domestique | Abeille | Common | Jour | Ambre doré, bandes sombres |

**Rareté : 11 Common, 3 Uncommon** — Zone tutoriel, beaucoup de découvertes faciles (voulu).
**Palette : verts, blancs, rouges, or, rose — jardin classique et accueillant**

---

### Zone 2 — Meadow 🌻
*Prairie de fleurs sauvages, herbes dorées, espace ouvert, vent*
**11 espèces** | Débloquée ~15 min | ~5-6 plantes

| # | Espèce | FR | Catégorie | Rareté | Actif | Couleur dominante |
|---|--------|----|-----------|--------|-------|-------------------|
| 15 | Clouded Sulphur | Coliade du trèfle | Papillon | Common | Jour | Jaune citron vif |
| 16 | Common Blue | Azuré commun | Papillon | Common | Jour | Bleu-violet (mâle) / brun+orange (femelle) |
| 17 | Monarch | Monarque | Papillon | Uncommon | Jour | Orange profond, veines noires |
| 18 | Gulf Fritillary | Fritillaire du Golfe | Papillon | Uncommon | Jour | Orange flamboyant, points argent |
| 19 | Old World Swallowtail | Machaon | Papillon | Rare | Jour | Jaune pâle, veines noires, tache rouge |
| 20 | Cinnabar Moth | Goutte de sang | P. de nuit | Common | Jour | Noir ardoise + rouge vif |
| 21 | Wool Carder Bee | Abeille cotonnière | Abeille | Uncommon | Jour | Noir + points jaunes |
| 22 | Long-Horned Bee | Eucère à longues antennes | Abeille | Uncommon | Jour | Sombre, très longues antennes (mâle) |
| 23 | Meadow Grasshopper | Criquet des pâtures | Sauterelle | Common | Jour | Vert (morphes brun/jaune/violet rare) |
| 24 | Band-Winged Grasshopper | Oedipode | Sauterelle | Uncommon | Jour | Gris-brun → ailes jaune/orange en vol |
| 25 | Six-Spotted Tiger Beetle | Cicindèle à 6 points | Coléoptère | Uncommon | Jour | Émeraude métallique, 6 points blancs |

**Rareté : 4 Common, 6 Uncommon, 1 Rare**
**Palette : jaunes, oranges, violets, émeraude — couleurs prairie**

---

### Zone 3 — Forest 🌲
*Lumière tamisée, mousse, fougères, champignons, sous-bois*
**11 espèces** | Débloquée ~30 min | ~4-5 plantes

| # | Espèce | FR | Catégorie | Rareté | Actif | Couleur dominante |
|---|--------|----|-----------|--------|-------|-------------------|
| 26 | Eastern Tiger Swallowtail | Papillon tigré de l'Est | Papillon | Uncommon | Jour | Jaune + rayures noires, taches bleues |
| 27 | Question Mark | Point d'interrogation | Papillon | Rare | Jour | Orange-brun, bords découpés, « ? » argenté |
| 28 | Peppered Moth | Phalène du bouleau | P. de nuit | Common | Nuit | Blanc moucheté noir OU forme sombre |
| 29 | Luna Moth | Papillon lune | P. de nuit | Rare | Nuit | Vert lime éthéré, longues queues |
| 30 | Elephant Hawk-Moth | Grand sphinx de la vigne | P. de nuit | Uncommon | Crépuscule | Olive + rose vif |
| 31 | Stag Beetle | Lucane cerf-volant | Coléoptère | Uncommon | Crépuscule | Brun-noir, mandibules en bois de cerf |
| 32 | Ebony Jewelwing | Caloptéryx maculé | Demoiselle | Uncommon | Jour | Corps émeraude, ailes entièrement noires |
| 33 | Walking Stick | Phasme | Autre | Rare | Nuit | Brindille brune ou verte |
| 34 | Annual Cicada | Cigale caniculaire | Autre | Uncommon | Jour | Vert foncé, ailes transparentes |
| 35 | Spotted Lanternfly | Fulgore tacheté | Autre | Common | Jour | Gris+points noirs → rouge vif (ouvert) |
| 36 | Mason Bee | Abeille maçonne | Abeille | Common | Jour | Bleu-noir métallique ou roux |

**Rareté : 3 Common, 5 Uncommon, 3 Rare**
**Palette : verts sombres, bruns, éclats de lime/rose — lumière tamisée**

---

### Zone 4 — Deep Wood 🪵
*Bûches moussues, compost, champignons, décomposition*
**9 espèces** | Débloquée ~1h | ~2-3 plantes + bûches/compost

| # | Espèce | FR | Catégorie | Rareté | Actif | Couleur dominante |
|---|--------|----|-----------|--------|-------|-------------------|
| 37 | Polyphemus Moth | Polyphème d'Amérique | P. de nuit | Rare | Nuit | Fauve, taches oculaires pourpre-jaune |
| 38 | Cecropia Moth | Saturnie cécropia | P. de nuit | Very Rare | Nuit | Brun-rouge-blanc, croissants |
| 39 | Death's-head Hawkmoth | Sphinx tête de mort | P. de nuit | Very Rare | Nuit | Brun velouté, crâne, bandes jaune-orange |
| 40 | Rhinoceros Beetle | Scarabée rhinocéros | Coléoptère | Rare | Nuit | Brun-rouge brillant, corne courbée |
| 41 | Colorado Potato Beetle | Doryphore | Coléoptère | Common | Jour | Crème-jaune, 10 rayures noires |
| 42 | Weevil (Myllocerus) | Charançon vert | Coléoptère | Common | Jour | Vert pâle/blanc moucheté, museau |
| 43 | Carpenter Bee | Abeille charpentière | Abeille | Uncommon | Jour | Noir brillant, ailes iridescentes |
| 44 | Firefly | Luciole | Coléoptère | Uncommon | Nuit | Sombre, pronotum orange, lanterne verte |
| 45 | Leaf Insect | Phyllie | Autre | Legendary | Nuit | Vert feuille, nervures, bords « mordus » |

**Rareté : 2 Common, 2 Uncommon, 2 Rare, 2 Very Rare, 1 Legendary**
**Palette : bruns, verts mousse, éclats bioluminescents — forêt profonde**
**Mécanique spéciale : bûches à stades de décomposition, compost à retourner**

---

### Zone 5 — Rock Garden ⛰️
*Pierres empilées, fleurs alpines en coussin, ciel ouvert, lichens*
**9 espèces** | Débloquée ~1h30 | ~3-4 plantes + pierres/crevasses

| # | Espèce | FR | Catégorie | Rareté | Actif | Couleur dominante |
|---|--------|----|-----------|--------|-------|-------------------|
| 46 | Black Swallowtail | Porte-queue noir | Papillon | Uncommon | Jour | Noir, points jaunes, iridescence bleue |
| 47 | Glasswing | Papillon de verre | Papillon | Very Rare | Jour | Ailes TRANSPARENTES, bordure brun-orange |
| 48 | Sacred Scarab | Bousier sacré | Coléoptère | Rare | Jour | Noir large, crête dentée |
| 49 | Teddy Bear Bee | Abeille nounours | Abeille | Rare | Jour | Fourrure dorée-brune dense |
| 50 | Leafcutter Bee | Mégachile | Abeille | Uncommon | Jour | Sombre, bandes pâles, porte des feuilles |
| 51 | Cone-Headed Grasshopper | Conocéphale | Sauterelle | Uncommon | Nuit | Vert vif, tête très conique |
| 52 | Field Cricket | Grillon des champs | Sauterelle | Common | Nuit | Noir brillant, longues antennes |
| 53 | Pillbug | Cloporte | Isopode* | Common | Jour | Gris ardoise, se roule en boule |
| 54 | Ant | Fourmi | Insecte social | Common | Jour | Noir/brun, piste en file |

*\*Le Pillbug est techniquement un isopode (crustacé), pas un insecte. Inclus pour le gameplay — le journal peut noter cette curiosité comme fun fact.*

**Rareté : 3 Common, 3 Uncommon, 2 Rare, 1 Very Rare**
**Palette : gris pierre, lichens jaunes, fleurs alpines, noir brillant**
**Mécanique spéciale : pierres créant des pièges à soleil, crevasses à explorer**

---

### Zone 6 — Pond 🐸
*Nénuphars, roseaux, eau calme, reflets*
**8 espèces** | Débloquée ~2h | ~2-3 plantes aquatiques + tuiles d'eau

| # | Espèce | FR | Catégorie | Rareté | Actif | Couleur dominante |
|---|--------|----|-----------|--------|-------|-------------------|
| 55 | Blue Dasher | Pachydiplax | Libellule | Common | Jour | Bleu poudreux, yeux verts |
| 56 | Azure Damselfly | Agrion jouvencelle | Demoiselle | Common | Jour | Bleu ciel, marques noires |
| 57 | Twelve-Spotted Skimmer | Libellule gracieuse | Libellule | Common | Jour | Damier 12 taches sombres + 8 blanches |
| 58 | Water Strider | Gerris | Autre | Common | Jour | Brun-noir, longues pattes évasées |
| 59 | Flame Skimmer | Libellule flamboyante | Libellule | Uncommon | Jour | Entièrement orange-rouge feu |
| 60 | Halloween Pennant | Libellule d'Halloween | Libellule | Uncommon | Jour | Ailes brun+ambre en bandes |
| 61 | Red-Veined Darter | Sympétrum à nervures rouges | Libellule | Uncommon | Jour | Rouge vif, nervures rouges |
| 62 | Emperor Dragonfly | Anax empereur | Libellule | Rare | Jour | Très grand, thorax vert, abdomen bleu |

**Rareté : 4 Common, 3 Uncommon, 1 Rare**
**Palette : bleus, verts aqua, ambre, rouge feu — aquatique**
**Mécanique spéciale : tuiles d'eau, roseaux comme terrain, bonus photo reflet**

---

### Zone 7 — Tropical Greenhouse 🌺
*Serre vitrée, orchidées, vignes, brume — zone endgame*
**10 espèces** | Débloquée ~3h | ~4-5 plantes exotiques

| # | Espèce | FR | Catégorie | Rareté | Actif | Couleur dominante |
|---|--------|----|-----------|--------|-------|-------------------|
| 63 | Zebra Longwing | Hélioconie zébrée | Papillon | Rare | Jour | Noir, rayures jaunes audacieuses |
| 64 | Ulysses Butterfly | Papillon Ulysse | Papillon | Very Rare | Jour | Bleu iridescent brillant |
| 65 | Queen Alexandra's Birdwing | Ornithoptère de la Reine Alexandra | Papillon | Legendary | Jour | Mâle bleu-vert, plus grand papillon |
| 66 | Atlas Moth | Bombyx atlas | P. de nuit | Very Rare | Nuit | Brun-rouge, fenêtres transparentes, pointes cobra |
| 67 | Comet Moth | Papillon comète | P. de nuit | Legendary | Nuit | Jaune brillant, queues de 15 cm |
| 68 | Madagascan Sunset Moth | Chrysiride de Madagascar | P. de nuit | Very Rare | Jour | Arc-en-ciel iridescent sur noir |
| 69 | Orchid Bee | Abeille des orchidées | Abeille | Very Rare | Jour | Vert/bleu/or métallique |
| 70 | Blue-Banded Bee | Abeille à bandes bleues | Abeille | Rare | Jour | Noir + bandes turquoise opalescentes |
| 71 | Hercules Beetle | Dynaste Hercule | Coléoptère | Legendary | Nuit | Olive-vert, thorax noir, énormes pinces |
| 72 | Orchid Mantis | Mante orchidée | Autre | Legendary | Jour | Blanc-rose, pattes en forme de pétales |

**Rareté : 0 Common, 0 Uncommon, 2 Rare, 4 Very Rare, 4 Legendary**
**Palette : tropicaux saturés — bleus, verts, roses, or, arc-en-ciel**
**Mécanique spéciale : brume, lampe UV, chrysalides à observer, station de fruits**

---

## Résumé des zones

| Zone | Espèces | C | U | R | VR | L | Plantes | Autres mécaniques |
|------|---------|---|---|---|----|----|---------|-------------------|
| Starter Garden | 14 | 11 | 3 | 0 | 0 | 0 | 5-6 | — (tutoriel) |
| Meadow | 11 | 4 | 6 | 1 | 0 | 0 | 5-6 | Vent/sway |
| Forest | 11 | 3 | 5 | 3 | 0 | 0 | 4-5 | Patches de lumière |
| Deep Wood | 9 | 2 | 2 | 2 | 2 | 1 | 2-3 | Bûches, compost |
| Rock Garden | 9 | 3 | 3 | 2 | 1 | 0 | 3-4 | Pierres, crevasses |
| Pond | 8 | 4 | 3 | 1 | 0 | 0 | 2-3 | Tuiles d'eau |
| Tropical Greenhouse | 10 | 0 | 0 | 2 | 4 | 4 | 4-5 | Brume, UV, chrysalides |
| **Total** | **72** | **27** | **22** | **11** | **7** | **5** | **~30** | |

---

## Espèces par catégorie

| Catégorie | Espèces | % du total |
|-----------|---------|------------|
| Papillons | 15 | 21% |
| Papillons de nuit | 12 | 17% |
| Coléoptères | 13 | 18% |
| Libellules & Demoiselles | 7 | 10% |
| Abeilles | 9 | 12.5% |
| Sauterelles & Grillons | 4 | 5.5% |
| Syrphes | 1 | 1.5% |
| Autres (mantis, phasme, etc.) | 8 | 11% |
| Isopode (Pillbug) | 1 | 1.5% |
| Insecte social (Ant) | 1 | 1.5% |
| **Total** | **72** | **100%** |

---

## Activité jour/nuit

| Période | Espèces | % |
|---------|---------|---|
| Jour | 51 | 71% |
| Nuit | 16 | 22% |
| Crépuscule | 5 | 7% |

**Nuit par zone :** Starter (2), Forest (3), Deep Wood (5), Rock Garden (2), Tropical (3), Pond (0), Meadow (0)

---

## Plantes — Répartition cible (~30)

### Starter Garden (5-6 plantes)
Plantes de jardin classiques : lavande, tournesol, marguerite, œillet d'Inde, souci, onagre (nuit).
*Rôle : apprentissage de la mécanique de plantation, attrait d'insectes Common.*

### Meadow (5-6 plantes)
Fleurs sauvages : asclépiade (milkweed → Monarch obligatoire), verge d'or, trèfle, bleuet, séneçon (→ Cinnabar Moth), chardon.
*Rôle : combinaisons de plantes pour insectes Uncommon.*

### Forest (4-5 plantes)
Sous-bois : fougère, muguet, digitale, violette, chèvrefeuille.
*Rôle : plantes d'ombre, certaines attirent des papillons de nuit.*

### Deep Wood (2-3 plantes + mécaniques)
Peu de plantes cultivées : champignons (décor), mousse. Bûches à 3 stades de décomposition + tas de compost.
*Rôle : les insectes sont attirés par le bois mort et la décomposition, pas les fleurs.*

### Rock Garden (3-4 plantes + mécaniques)
Fleurs alpines : thym, edelweiss, saxifrage, lavande de mer. Pierres chauffées par le soleil.
*Rôle : plantes résistantes, mécanique de piège à soleil.*

### Pond (2-3 plantes + eau)
Plantes aquatiques : nénuphar, quenouille, iris d'eau. Tuiles d'eau comme terrain principal.
*Rôle : les libellules dépendent de l'eau, pas des plantes cultivées.*

### Tropical Greenhouse (4-5 plantes)
Exotiques : orchidée, passiflore, hibiscus, lantana, bougainvillier.
*Rôle : plantes de haute valeur pour insectes rares/légendaires.*

**Total estimé : ~28-32 plantes** (à affiner lors du sprint plantes)

---

## Liste alphabétique complète — Référence rapide

| # | Espèce | FR | Catégorie | Zone | Rareté | Actif |
|---|--------|----|-----------|------|--------|-------|
| 1 | Annual Cicada | Cigale caniculaire | Autre | Forest | Uncommon | Jour |
| 2 | Ant | Fourmi | Social | Rock Garden | Common | Jour |
| 3 | Atlas Moth | Bombyx atlas | P. de nuit | Tropical | Very Rare | Nuit |
| 4 | Azure Damselfly | Agrion jouvencelle | Demoiselle | Pond | Common | Jour |
| 5 | Band-Winged Grasshopper | Oedipode | Sauterelle | Meadow | Uncommon | Jour |
| 6 | Black Swallowtail | Porte-queue noir | Papillon | Rock Garden | Uncommon | Jour |
| 7 | Blue Dasher | Pachydiplax | Libellule | Pond | Common | Jour |
| 8 | Blue-Banded Bee | Abeille à bandes bleues | Abeille | Tropical | Rare | Jour |
| 9 | Cabbage White | Piéride du chou | Papillon | Starter | Common | Jour |
| 10 | Carpenter Bee | Abeille charpentière | Abeille | Deep Wood | Uncommon | Jour |
| 11 | Cecropia Moth | Saturnie cécropia | P. de nuit | Deep Wood | Very Rare | Nuit |
| 12 | Cinnabar Moth | Goutte de sang | P. de nuit | Meadow | Common | Jour |
| 13 | Clouded Sulphur | Coliade du trèfle | Papillon | Meadow | Common | Jour |
| 14 | Colorado Potato Beetle | Doryphore | Coléoptère | Deep Wood | Common | Jour |
| 15 | Comet Moth | Papillon comète | P. de nuit | Tropical | Legendary | Nuit |
| 16 | Common Blue | Azuré commun | Papillon | Meadow | Common | Jour |
| 17 | Cone-Headed Grasshopper | Conocéphale | Sauterelle | Rock Garden | Uncommon | Nuit |
| 18 | Death's-head Hawkmoth | Sphinx tête de mort | P. de nuit | Deep Wood | Very Rare | Nuit |
| 19 | Eastern Tiger Swallowtail | Papillon tigré de l'Est | Papillon | Forest | Uncommon | Jour |
| 20 | Ebony Jewelwing | Caloptéryx maculé | Demoiselle | Forest | Uncommon | Jour |
| 21 | Elephant Hawk-Moth | Grand sphinx de la vigne | P. de nuit | Forest | Uncommon | Crépuscule |
| 22 | Emperor Dragonfly | Anax empereur | Libellule | Pond | Rare | Jour |
| 23 | European Mantis | Mante religieuse | Autre | Starter | Uncommon | Jour |
| 24 | Field Cricket | Grillon des champs | Sauterelle | Rock Garden | Common | Nuit |
| 25 | Firefly | Luciole | Coléoptère | Deep Wood | Uncommon | Nuit |
| 26 | Flame Skimmer | Libellule flamboyante | Libellule | Pond | Uncommon | Jour |
| 27 | Garden Tiger Moth | Écaille martre | P. de nuit | Starter | Common | Nuit |
| 28 | Glasswing | Papillon de verre | Papillon | Rock Garden | Very Rare | Jour |
| 29 | Golden Tortoise Beetle | Casside dorée | Coléoptère | Starter | Uncommon | Jour |
| 30 | Green Lacewing | Chrysope verte | Autre | Starter | Common | Crépuscule |
| 31 | Gulf Fritillary | Fritillaire du Golfe | Papillon | Meadow | Uncommon | Jour |
| 32 | Halloween Pennant | Libellule d'Halloween | Libellule | Pond | Uncommon | Jour |
| 33 | Hercules Beetle | Dynaste Hercule | Coléoptère | Tropical | Legendary | Nuit |
| 34 | Japanese Beetle | Scarabée japonais | Coléoptère | Starter | Common | Jour |
| 35 | Leaf Insect | Phyllie | Autre | Deep Wood | Legendary | Nuit |
| 36 | Leafcutter Bee | Mégachile | Abeille | Rock Garden | Uncommon | Jour |
| 37 | Long-Horned Bee | Eucère à longues antennes | Abeille | Meadow | Uncommon | Jour |
| 38 | Luna Moth | Papillon lune | P. de nuit | Forest | Rare | Nuit |
| 39 | Madagascan Sunset Moth | Chrysiride de Madagascar | P. de nuit | Tropical | Very Rare | Jour |
| 40 | Marmalade Hoverfly | Syrphe ceinturé | Syrphe | Starter | Common | Jour |
| 41 | Mason Bee | Abeille maçonne | Abeille | Forest | Common | Jour |
| 42 | Meadow Grasshopper | Criquet des pâtures | Sauterelle | Meadow | Common | Jour |
| 43 | Monarch | Monarque | Papillon | Meadow | Uncommon | Jour |
| 44 | Old World Swallowtail | Machaon | Papillon | Meadow | Rare | Jour |
| 45 | Orange Tip | Aurore | Papillon | Starter | Common | Jour |
| 46 | Orchid Bee | Abeille des orchidées | Abeille | Tropical | Very Rare | Jour |
| 47 | Orchid Mantis | Mante orchidée | Autre | Tropical | Legendary | Jour |
| 48 | Peppered Moth | Phalène du bouleau | P. de nuit | Forest | Common | Nuit |
| 49 | Pillbug | Cloporte | Isopode | Rock Garden | Common | Jour |
| 50 | Polyphemus Moth | Polyphème d'Amérique | P. de nuit | Deep Wood | Rare | Nuit |
| 51 | Queen Alexandra's Birdwing | Ornithoptère de la Reine Alexandra | Papillon | Tropical | Legendary | Jour |
| 52 | Question Mark | Point d'interrogation | Papillon | Forest | Rare | Jour |
| 53 | Red Admiral | Vulcain | Papillon | Starter | Uncommon | Jour |
| 54 | Red-Veined Darter | Sympétrum à nervures rouges | Libellule | Pond | Uncommon | Jour |
| 55 | Rhinoceros Beetle | Scarabée rhinocéros | Coléoptère | Deep Wood | Rare | Nuit |
| 56 | Rose Chafer | Cétoine dorée | Coléoptère | Starter | Common | Jour |
| 57 | Rosy Maple Moth | Anisote de l'érable | P. de nuit | Starter | Common | Nuit |
| 58 | Sacred Scarab | Bousier sacré | Coléoptère | Rock Garden | Rare | Jour |
| 59 | Seven-Spot Ladybug | Coccinelle à 7 points | Coléoptère | Starter | Common | Jour |
| 60 | Six-Spotted Tiger Beetle | Cicindèle à 6 points | Coléoptère | Meadow | Uncommon | Jour |
| 61 | Spotted Lanternfly | Fulgore tacheté | Autre | Forest | Common | Jour |
| 62 | Stag Beetle | Lucane cerf-volant | Coléoptère | Forest | Uncommon | Crépuscule |
| 63 | Teddy Bear Bee | Abeille nounours | Abeille | Rock Garden | Rare | Jour |
| 64 | Twelve-Spotted Skimmer | Libellule gracieuse | Libellule | Pond | Common | Jour |
| 65 | Ulysses Butterfly | Papillon Ulysse | Papillon | Tropical | Very Rare | Jour |
| 66 | Walking Stick | Phasme | Autre | Forest | Rare | Nuit |
| 67 | Water Strider | Gerris | Autre | Pond | Common | Jour |
| 68 | Weevil (Myllocerus) | Charançon vert | Coléoptère | Deep Wood | Common | Jour |
| 69 | Western Honeybee | Abeille domestique | Abeille | Starter | Common | Jour |
| 70 | Wool Carder Bee | Abeille cotonnière | Abeille | Meadow | Uncommon | Jour |
| 71 | Zebra Longwing | Hélioconie zébrée | Papillon | Tropical | Rare | Jour |
| 72 | 22-Spot Ladybug | Coccinelle à 22 points | Coléoptère | Starter | Common | Jour |
