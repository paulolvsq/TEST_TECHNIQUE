# Distributor

Outil d'analyse des coûts de distribution et de planification. Des entrepôts expédient des
marchandises vers des magasins sur des périodes mensuelles, chacune ayant ses propres capacités
d'entrepôt, demandes de magasin et coûts de route.

## Aperçu de l'application

Application console .NET 10 avec deux fonctionnalités :

1. **Évaluer les scénarios** : Fonctionnalité de requête qui évalue le coût maximal de
   distribution selon des scénarios tarifaires sur une plage de périodes donnée. Les scénarios
   définissent des multiplicateurs hypothétiques au niveau des entrepôts et des magasins. Un
   scénario de base (sans ajustements) est toujours inclus à des fins de comparaison. La
   fonctionnalité utilise la multiplication matricielle parallélisée pour calculer les coûts
   sur toutes les combinaisons entrepôt-magasin-scénario.

2. **Planifier la distribution** : Fonctionnalité de commande qui planifie l'allocation optimale
   des expéditions sur une plage de périodes donnée à l'aide de la programmation linéaire
   (solveur HiGHS) et sauvegarde le plan dans la base de données. Pour chaque période, détermine
   la distribution de marchandises des entrepôts vers les magasins qui minimise le coût total, en
   respectant les contraintes de capacité et de demande.

Dépendances principales :

- `Microsoft.EntityFrameworkCore.Sqlite` : fournisseur Entity Framework Core pour SQLite
- `System.CommandLine` : framework d'interface en ligne de commande
- `MathNet.Numerics` : multiplication matricielle
- `Highs.Native` : interface .NET pour le solveur d'optimisation linéaire HiGHS

## Domaine

Le réseau de distribution est composé d'**entrepôts** et de **magasins** reliés par des
**routes**. Si un entrepôt et un magasin sont reliés par une route, l'entrepôt peut expédier
des marchandises vers ce magasin.

Une **période** représente un mois, par exemple `2026-01`. Durant chaque période, chaque
entrepôt a une **capacité** (nombre maximal d'unités qu'il peut expédier), chaque magasin a une
**demande** (nombre total d'unités qu'il doit recevoir), et chaque route a un **coût unitaire**
(coût par unité expédiée le long de cette route).

Un **scénario** modélise une situation hypothétique en appliquant des multiplicateurs aux
entrepôts et aux magasins. Un ajustement d'entrepôt multiplie la contribution en coût de cet
entrepôt. Un ajustement de magasin multiplie la demande de ce magasin. Les scénarios sont
comparés à un scénario de base (sans ajustements) pour évaluer l'impact en coût de différentes
conditions possibles.

Un **plan de distribution** pour une plage de périodes donnée consiste en un ensemble
d'**expéditions** attribuant des quantités d'unités spécifiques des entrepôts vers les magasins
pour chaque période, en minimisant le coût total tout en respectant les contraintes de capacité
et de demande.

## Démarrage

### 1. SDK .NET

Télécharger et installer le [SDK .NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).

### 2. Alimenter la base de données

Alimenter le fichier de base de données SQLite en exécutant le programme d'alimentation avec la
commande `seed` ou `reset`. La base de données doit être alimentée avant d'exécuter
l'application.

```
dotnet run --project Distributor.Seeder -- seed --small
dotnet run --project Distributor.Seeder -- reset --large
```

Exécuter l'une ou l'autre commande avec `--help` pour afficher les détails d'utilisation.

Les profils de lancement dans `Distributor.Seeder/Properties/launchSettings.json` permettent
également d'exécuter le programme directement depuis votre IDE.

Le fichier de base de données est stocké à :

- Windows : `%LOCALAPPDATA%/Distributor/distributor.db`
- Linux / macOS : `~/.local/share/Distributor/distributor.db`

### 3. Exécuter l'application

Évaluer les scénarios :

```
dotnet run --project Distributor -- evaluate --start 2026-01 --end 2026-03 --scenarios 1 3 5
```

Planifier la distribution :

```
dotnet run --project Distributor -- plan --start 2026-01 --end 2026-03
```

Exécuter l'une ou l'autre commande avec `--help` pour afficher les détails d'utilisation.

Les profils de lancement dans `Distributor/Properties/launchSettings.json` permettent également
d'exécuter l'application directement depuis votre IDE.

### 4. Tester l'application

```
dotnet test
```

## Instructions de l'exercice

Prévoir environ 6 heures au total.

### Partie 1 : Corriger la fonctionnalité d'évaluation des scénarios

La commande `evaluate` contient des bogues dans trois domaines :

- **Bogues de concurrence** dans `MatrixMultiplier` et `MatrixSpanMultiplier`. La multiplication
  matricielle est conçue pour diviser les matrices d'entrée en sous-matrices (spans) et
  multiplier les spans correspondants en parallèle pour remplir la matrice de résultat.
  L'implémentation actuelle contient des bogues qui empêchent l'exécution parallèle correcte.

- **Bogues de construction matricielle** dans la fabrique de matrices de scénarios
  (`Distributor/Features/EvaluateScenarios/ScenarioMatrixFactory.cs`). La fabrique construit la
  matrice de coûts et les matrices de multiplicateurs utilisées dans l'évaluation. Vérifier les
  dimensions des matrices et les valeurs par défaut.

- **Bogues de logique métier** dans le gestionnaire de requêtes
  (`Distributor/Features/EvaluateScenarios/EvaluateScenariosQueryHandler.cs`). Le gestionnaire
  orchestre le chargement des périodes, la construction des matrices et l'assemblage des
  résultats. Vérifier comment les données circulent d'une période à l'autre.

Trouver et corriger tous les bogues. La fonctionnalité doit toujours produire des résultats
corrects pour toute requête donnée. La multiplication matricielle doit multiplier les spans
(sous-matrices) en parallèle.

Tout bogue découvert doit être accompagné de tests qui le démontrent et le préviennent. De tels
tests existent peut-être déjà. Sinon, les écrire vous-même.

### Partie 2 : Implémenter la fonctionnalité de planification de distribution

Le gestionnaire de commande `plan` (`PlanDistributionCommandHandler.HandleAsync`) n'est pas
implémenté. La signature de la méthode, la classe, le constructeur et toutes les dépendances
sont en place.

Implémenter selon la spécification suivante :

1. Charger les périodes dans la plage de dates demandée depuis la base de données. Lever une
   exception si aucune n'est trouvée.
2. Charger les entrepôts et les magasins référencés par ces périodes.
3. Pour chaque période, appeler `ITransportSolver.Solve(period)` pour calculer les expéditions
   optimales.
4. Construire les DTOs de résultat pour chaque période. Un `PeriodResult` contient la date de
   la période, le coût total et un tableau d'entrées `ShipmentResult`. Le coût de chaque
   expédition est `units * unitCost`, où le coût unitaire provient des coûts de route de la
   période pour cette paire entrepôt-magasin. Utiliser `decimal` pour toutes les valeurs de
   coût.
5. Persister une entité `DistributionPlan` (avec toutes les expéditions de toutes les périodes)
   dans la base de données via `DistributorDatabaseContext`.
6. Retourner un `PlanDistributionResult` avec l'identifiant du plan, la plage de dates, les
   détails par période et le coût total.

Se référer aux entités du domaine (`Period`, `DistributionPlan`, `Shipment`), aux dépôts, et au
gestionnaire de la fonctionnalité d'évaluation des scénarios (une fois corrigé) pour les patrons
et conventions utilisés dans le code.

### Partie 3 : Améliorer la qualité du code

Vous avez carte blanche pour améliorer le code de la manière que vous jugez appropriée. La
priorité est la fonctionnalité correcte, mais faites ensuite ce que vous pouvez pour améliorer
la maintenabilité, la lisibilité, la performance et les autres qualités non fonctionnelles sans
briser la fonctionnalité ni causer l'échec des tests.

## Critères d'évaluation

- Exactitude, performance et clarté de la fonctionnalité d'évaluation des scénarios, après
  correction des bogues
- Exactitude, performance et clarté de l'implémentation de la fonctionnalité de planification
  de distribution
- Couverture de tests : les bogues sont démontrés par des tests, le nouveau code est bien testé,
  tous les tests passent
- Améliorations de la qualité du code
