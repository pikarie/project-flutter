# Instructions pour Claude

## Source de vérité
- Le fichier `Plans/GDD.md` est la source de vérité du projet.
- Toujours le consulter avant d'implémenter une fonctionnalité.
- Le mettre à jour si des décisions changent en cours de développement.

## Conventions de commit
- Ne JAMAIS faire de commit. L'utilisateur vérifie et commit manuellement.
- Ne JAMAIS ajouter la ligne "Co-Authored-By" dans les messages de commit suggérés.
- Suivre la convention **Conventional Commits** : `type(scope): description`
  - `feat` : nouvelle fonctionnalité
  - `fix` : correction de bug
  - `docs` : documentation
  - `chore` : maintenance, config, setup
  - `refactor` : restructuration sans changement de comportement
  - `style` : formatage
  - `test` : tests

## Maintenance
- Supprimer automatiquement les fichiers `nul` (artefacts Windows) dès qu'ils apparaissent.

## Code
- Utiliser **C#** (dernière version supportée par Godot, soit C# 12 / .NET 8).
- Ne JAMAIS utiliser GDScript.
- **Noms de variables complets** : écrire les noms au long (`camera` au lieu de `cam`, `distance` au lieu de `dist`, `gameManager` au lieu de `gm`). Pas d'abréviations sauf conventions universelles (`i`/`j` pour boucles, `x`/`y` pour coordonnées).

## Langue
- Communiquer en français.
- Le code et les commits sont en anglais.
