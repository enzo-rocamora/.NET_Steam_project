# Jeu (Godot, UNITY, Winform, Console, …)

> Le jeu a été testé sur Godot 4.3.stable en dedug et exporté sur Windows avec 4 joueurs (sur la même machine).
> Il y a normalement dans ce dossier une configuration pour compiler et exporter le jeu en .exe, je n'ai eu aucun problème avec même pendant le transfert du WebServer au Client.
> Nom de la config : Windows Desktop (runnable) avec embed PCK pour avoir tout dans l'executable.

Le client est un peu capricieux par rapport à Gauniv.GameServer donc pour éviter tout problème, il est préférable de lancer le serveur avant le client.
Toutes les fonctionnalités demandées sont implémentées.


Attention, il n'est souvent pas possible de revenir en arrière, donc il est préférable de ne pas cliquer n'importe où, sous peine de devoir relancer le jeu.
Quitter le jeu gère la déconnexion avec le serveur.

### Commun

1. Entrer des identifiant de connexion - Done!
2. Sélection du nom - Done!
3. Ready check - Done!

# MJ ou #JOUEUR

1. Attente des autres joueurs - Done!
2. Affichage des résultats - Done!

### MJ

1. Sélection d’une case - Done!
2. Validation de la case sélectionné ou changement (ref #4) - Done!

### Joueur

1. Attente du choix du MJ - Done!
2. Affichage de la case sélectionné par le MJ - Done!
3. Clic ! - Done!

## Option

- Ajout d’un temp maximal pour cliquer - Done!
- Géré les joueurs dans la liste d’ami avec le statut correspondant - gérer par Gauniv.Client
- Remplacer le damier par une map créer par le MJ - Done !
