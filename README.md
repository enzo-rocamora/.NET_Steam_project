# Plateforme de distribution de contenu + Editeur

## Authors 
- [ROCAMORA Enzo](https://github.com/uncyzer)

> Le readme a été modifié et contient des informations complémentaires et des explications sur ce qui a été fait.
> Ici vous pouvez trouver des informations par rapport à Gauniv.WebServer.
> Gauniv.Client, Gauniv.Game et Gauniv.GameServer ont leurs propres readme dans leurs dossiers.
> Pour faire plus court la grand majorité du readme de base a été supprimé mais tout a été pris en compte.

## But

Construire un web service avec son client Windows pour gérer une plateforme de distribution de contenu limitée aux jeux vidéo. 

Ajouter à celui-ci un jeu multijoueur comprenant le serveur ainsi que le jeu correspondant.t

## A rendre

Un web service de stockage et de gestion des jeux en ligne.

Un logiciel sous Windows pour parcourir les jeux, en télécharger un et jouer à celui-ci.

Un serveur de jeu orchestrant le fonctionnement d’au moins un jeu.

Une application permettant de jouer à un jeu.

# Contrainte

Langages autorisés : C#, HTML, Javascript, CSS, TypeScript

Serveur web : ASP.Net Core

Logiciel Windows : WPF

Serveur de jeux : C#

Jeu : C# avec Godot, Unity, Winform, WPF, MAUI, ...

## Projet de départ

Votre solution devra être basée sur le projet Library.sln.

La partie serveur est dans le projet Gauniv.WebServer.

La partie client est dans le projet Gauniv.Client.

La connexion entre votre client et votre serveur est dans le projet Gauniv.Network.

Vous devrez créer les deux projets pour le serveur de jeu et le jeu lui-même.

Le serveur de jeu devra se nommer Gauniv.GameServer.

Le jeu devra se nommer Gauniv.Game.

# Service administration
Ce service correspond à l'administration de notre application, dans ce service nous gérons les jeux, les catégories, les utilisateurs.
## Les vues
- /game : Contient toute la partie de gestion des jeux, la page de base (index) contient une liste basique
  - Vues authentifiées :
    - Administrateur
      - /game/form : Partie de création d'un jeu 
      - /game/form/{id} (accessible quand on clique sur edit) : Partie de modification d'un jeux
      - /game/delete/{id} (accessible quand on clique sur delete) : Page de confirmation de suppression d'un jeu (avec quelques informations sur le jeu)
    - User :
      - ?showOwnedOnly=[true, false] : Affichage des jeux achetés
  - Vues non authentifiés :
    - ?sortBy=[name, name_desc, price, price_desc, date, date_desc] : Fonction de tri pour afficher les jeux dans un ordre particulier (clique sur le titre d'une colonne)
    - ?categories=33 OR ?categories=33&categories=50 : Tri en fonction des catégories.
    - La vue de base n'est pas authentifiée

- /category : Contient toute la partie de gestion des catégories
  - Vues authentifiées (administrateurs):
    - /category/form : Partie de création d'une catégorie 
    - /category/form/{id} (accessible quand on clique sur edit) : Partie de modification d'unee catégorie
    - /category/delete/{id} (accessible quand on clique sur delete) : Page de confirmation de suppression d'une catégorie (donne le nombre de jeux impactés)
  - Vues non authentifiés :
    - /category : Liste toutes les catégories
- /user : Contient toutes les informations des utilisateurs (Toutes les vues doivent être authentifiées en administrateur)
  - /user/detail/{id} : Vue détaillé d'un utilisateur (en cliquant sur détails)
- /statistics : Page de statiques (accessible à tout le monde).
  - /statistics/categorydetails : en cliquant sur Details en bas dans la liste des catégories
  - /statistics/gamedetails : en cliquant sur Details dans les détails d'une catégorie ou en recherchant un jeu dans la barre de recherche
- /status : Page publique, contenant le status de tout les utilisateurs au monde, avec un filtre en fonction des états

## L'API
### Routes `/api/games`

### Public Routes

#### `GET /api/games`
Optional Parameters:
- `offset` (pagination)
- `limit` (pagination)
- `sortBy=[name, name_desc, price, price_desc, date, date_desc]`
- `categories[]` (one or more category IDs)

#### Authenticated Routes

#### `GET /api/games?owned=true`
- Same parameters as public route
- Requires Bearer Token
- Returns only games owned by the user

#### `GET /api/games/{id}/download`
- Requires Bearer Token
- Verifies game ownership
- Returns game file as stream

### Routes `/api/categories`

### Public Routes

#### `GET /api/categories`
- Lists all categories
- No required parameters

### Routes `/api/user`

### Authenticated Routes (Bearer Token Required)

#### `GET /api/user/games`
Optional Parameters:
- `offset` (pagination)
- `limit` (pagination)
- `categories[]` (category filtering)

#### `POST /api/user/games/{gameId}`
- Purchases specified game for authenticated user

#### `GET /api/user/friends`
- Lists authenticated user's friends

#### `POST /api/user/friends`
Body:
```json
{
  "id?": "string",
  "email?": "string",
  "userName?": "string"
}
```
- Adds a friend to the authenticated user

### Routes `/api/user/status`

### Authenticated Routes (Bearer Token Required)

#### `GET /api/user/status`
Returns:
```json
{
  "status": "string",
  "lastSeen": "datetime",
  "currentGame": {
    "gameId": "number",
    "title": "string",
    "startedAt": "datetime"
  } | null
}
```

#### `POST /api/user/status/game/start/{gameId}`
- Indicates user starts playing specified game

#### `POST /api/user/status/game/stop`
- Indicates user stops playing

### Response Objects

### Game Object
```json
{
  "id": "number",
  "title": "string",
  "description": "string",
  "price": "decimal",
  "categories": "Category[]",
  "createdAt": "datetime",
  "updatedAt": "datetime"
}
```

### Category Object
```json
{
  "id": "number",
  "name": "string",
  "description": "string"
}
```

### User Game Object
```json
{
  "id": "number",
  "title": "string",
  "purchaseDate": "datetime",
  "totalPlayTime": "timespan",
  "lastPlayedAt": "datetime",
  "categories": "string[]"
}
```

### Friend Object
```json
{
  "id": "string",
  "userName": "string",
  "fullName": "string",
  "status": "Online | Offline | InGame",
  "lastSeen": "datetime",
  "currentGame": {
    "gameId": "number",
    "title": "string",
    "startedAt": "datetime"
  } | null
}
```

### Paginated Response
All paginated routes return:
```json
{
  "items": "T[]",
  "totalCount": "number",
  "pageSize": "number",
  "currentPage": "number",
  "totalPages": "number",
  "hasNext": "boolean",
  "hasPrevious": "boolean"
}
```

### HTTP Status Codes

| Code | Description |
|------|-------------|
| 200  | Success |
| 400  | Bad Request |
| 401  | Unauthorized |
| 403  | Forbidden |
| 404  | Not Found |
| 500  | Server Error |

### Authentication
All authenticated routes require header:
```
Authorization: Bearer <token>
```

# Application (WPF, MAUI, WINUI)

Voir le [readme](Gauniv.Client/GaunivClient.md) qui se trouve dans le dossier Gauniv.Client !

# Serveur de jeu (Console)

Voir le [readme](Gauniv.GameServer/Gauniv.GameServer.md) qui se trouve dans le dossier Gauniv.GameServer !

# Jeu (Godot, UNITY, Winform, Console, …)

Voir le [readme](Gauniv.Game/Gauniv.Game.md) qui se trouve dans le dossier Gauniv.Game !
