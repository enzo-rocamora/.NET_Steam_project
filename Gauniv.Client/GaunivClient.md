
# Fonctionnalités de l'application

## Interface Utilisateur et Navigation :

- **Pages de navigation** : Une structure de navigation est en place, permettant un accès facile entre les différentes sections, incluant la page d'accueil, les détails de chaque jeu, la bibliothèque de l'utilisateur, le profil et le magasin de jeux.
- **Pages spécifiques** : Chaque page (Accueil, Mes Jeux, Profil, Magasin) est conçue avec des vues XAML dédiées, accompagnées de leur logique pour une interactivité fluide.

## Gestion de Jeux :

- **Téléchargement des jeux** : Les utilisateurs peuvent télécharger des jeux, avec un suivi détaillé des états de téléchargement (en cours, terminé, ou en échec).
- **Bibliothèque de jeux** : Affiche la liste des jeux téléchargés ou possédés par l'utilisateur, avec la possibilité de lancer le jeu ou de l'arrêter.
- **Détails de jeux** : Chaque jeu a sa page de détails, affichant la description, et d'autres informations importantes pour l'utilisateur.
- **Filtres et tri avancés** : Options de filtrage (par nom, date de création, prix), barre de recherche pour chercher selon le nom du jeu, filtrage par catégories.

## Profil Utilisateur et Authentification :

- **Authentification sécurisée** : Système d'authentification incluant les fonctionnalités de connexion et de déconnexion, permettant de gérer l'accès utilisateur.
- **Gestion du profil** : Les utilisateurs peuvent consulter leur profil, avec la possibilité d'ajouter des amis et de regarder sa liste d'ami pour vérifier leur statut.

## Services et Infrastructure :

- **Service d'authentification** : Gère la vérification des informations de connexion et la gestion des sessions utilisateur.
- **Service de téléchargement** : Responsable de l’exécution et du suivi des téléchargements de jeux, avec une gestion des erreurs pour assurer la fiabilité du processus.
- **Gestionnaire de processus de jeu** : Contrôle le lancement et la fermeture des jeux, garantissant une exécution stable.
- **Service de connectivité** : Gestion des connexions réseau pour garantir la continuité des fonctionnalités en ligne et la prise en charge des déconnexions.

### 1. Configuration Générale et Initialisation
- **App.xaml** : Ce fichier XAML contient les configurations principales de l'application, notamment le style et les ressources globales. Il inclut la définition des pages et des styles partagés dans l'application.
- **App.xaml.cs** : Il initialise les services et la configuration de l'application, en configurant les composants à démarrer au lancement de l'application.
- **AppShell.xaml** : Définit la structure de la navigation globale, incluant les éléments de navigation, les pages de destination et les configurations de navigation.
- **AppShell.xaml.cs** : Fichier gérant les événements de navigation et d'autres actions utilisateur liées à la structure de l'application.
- **Gauniv.Client.csproj** : Fichier de configuration de projet. Il déclare les dépendances du projet, les frameworks ciblés et d'autres configurations importantes pour la compilation.

### 2. Modèles (Models)
Les modèles représentent les structures de données essentielles utilisées dans l'application :
- **GameDownloadState.cs** : Enumération définissant les différents états de téléchargement d'un jeu, comme `EnCours`, `Terminé`, ou `Erreur`, pour un suivi précis des téléchargements.
- **GameWithDownload.cs** : Modèle décrivant les informations d'un jeu avec des attributs comme l'état de téléchargement, le chemin de fichier, et d'autres détails spécifiques.
- **UserGameDto.cs** : DTO (Data Transfer Object) pour les jeux de l'utilisateur, facilitant la transmission de données entre la couche backend et l'interface utilisateur.
- **UserStatus.cs** : Modèle définissant le statut d'un utilisateur, incluant des informations de connexion, de statut en ligne, et d'autres détails utilisateurs pertinents.

### 3. Pages XAML
Chaque page XAML représente une interface utilisateur distincte et inclut la logique associée :
- **GameDetails.xaml** et **GameDetails.xaml.cs** : Affiche les détails d'un jeu spécifique, tels que la description, les images, les évaluations et permet des actions comme télécharger ou supprimer un jeu.
- **Index.xaml** et **Index.xaml.cs** : Page d'accueil de l'application, montrant les jeux populaires ou récemment ajoutés, avec des options de navigation rapide.
- **MyGames.xaml** et **MyGames.xaml.cs** : Affiche la liste des jeux téléchargés ou possédés par l'utilisateur, avec la possibilité de les gérer (ex: lancer, supprimer, mettre à jour).
- **Profile.xaml** et **Profile.xaml.cs** : Permet à l'utilisateur de visualiser et de mettre à jour son profil, incluant des informations de compte, des paramètres de confidentialité, etc.
- **Store.xaml** et **Store.xaml.cs** : Page du magasin où l'utilisateur peut explorer et rechercher des jeux disponibles, et les ajouter à sa collection.

### 4. Services
Les services sont responsables de la logique métier et de la gestion des données :
- **AuthenticationService.cs** : Gère l'authentification des utilisateurs, incluant la connexion, la déconnexion et la gestion de session.
- **GameDownloadService.cs** : Responsable du téléchargement des jeux, du suivi des états de téléchargement, et de la gestion des interruptions ou des erreurs.
- **GameProcessManager.cs** : Gère les processus des jeux, comme le lancement et la fermeture, assurant que les jeux sont correctement exécutés et fermés.
- **Navigation.cs** : Service central de navigation, facilitant le déplacement entre les pages en respectant la structure de navigation.
- **Network.cs** : Assure la gestion des connexions réseau et gère les exceptions en cas de perte de connexion, pour une expérience utilisateur fluide.
- **OnlineService.cs** : Gère les fonctionnalités en ligne comme les connexions serveur, les mises à jour de données en temps réel, et les communications réseau.

### 5. ViewModels
Les ViewModels assurent la liaison entre la vue (UI) et les modèles (données), en encapsulant la logique métier pour chaque vue :
- **GameDetailsViewModel.cs** : Contient la logique pour afficher et mettre à jour les informations de la page `GameDetails`, y compris les actions de téléchargement et de mise à jour.
- **IndexViewModel.cs** : Fournit les données et la logique pour la page d'accueil, affichant des jeux recommandés, populaires, ou récemment ajoutés.
- **MenuViewModel.cs** : Gère les interactions utilisateur du menu principal, en facilitant l'accès aux différentes sections de l'application.
- **MyGamesViewModel.cs** : Assure la gestion des jeux de l'utilisateur, incluant la récupération et l'affichage des jeux téléchargés, ainsi que les options de gestion.
- **ProfileViewModel.cs** : Manipule les données du profil utilisateur et permet des mises à jour de données personnelles et de paramètres de confidentialité.
- **StoreViewModel.cs** : Logiciel pour la page `Store`, permettant la navigation dans la liste des jeux, les recherches, et les actions d'achat ou de téléchargement.


