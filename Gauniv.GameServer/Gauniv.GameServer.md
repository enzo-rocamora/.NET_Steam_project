# Serveur de jeu (Console)

> Ce n'était pas précisé donc je n'ai pas rajouté Gauniv.GameServer dans le docker-compose
> Mais je confirme que tout fonctionne même quand le server est exporté via `Publish` en tant qu'executable

Toutes les fonctionnalités demandées sont implémentées.
Le serveur est capable de gérer plusieurs parties en même temps.
Il gère les déconnexions.
Il netttoie les parties terminées ou abandonnées (sans joueurs).

## Deroulement d’une partie

Le jeu se joue sur un damier N*N.

1. Le serveur attend que tous les joueurs soient prêts pour commencer la partie. - Done!
2. Le serveur décide du MJ et avertit tous les participants de leurs rôles. - Done!
3. Le MJ décide d'une case et valide son choix. - Done!
4. Les joueurs reçoivent le top départ. - Done!
5. Chaque joueur clique le plus vite possible sur la case choisie par le MJ. - Done!
6. Le serveur définit l'ordre final des joueurs grâce au temps de réaction de chaque joueur. - Done!        
7. Pour chaque joueur, le serveur vérifie que la participation du joueur est valide grâce à la fonction ci-dessous.
   Si le joueur est exclu, la position de tous les joueurs doit être mise à jour en conséquence. - Done!
8. Le serveur communique le résultat final à tout le monde. - Done!


## Verifier l’eligibilité d’un joueur

- Done!

```csharp
bool IsEligible(int pos, string name)
{
    Stopwatch sw = new();
    sw.Start();
    ECDsa key = ECDsa.Create();
    key.GenerateKey(ECCurve.NamedCurves.nistP521);
    int t = 5000 / pos;
    var k = new byte[t];
    var d = Encoding.UTF8.GetBytes(name);
    for (int i = 0; i < t; i++)
    {
        var s = key.SignData(d.Concat(BitConverter.GetBytes(pos)).ToArray(), HashAlgorithmName.SHA512);
        k[i] = s[i % s.Length];
    }
    var res = key.SignData(k, HashAlgorithmName.SHA512);
    sw.Stop();
    Console.WriteLine($"{pos} {sw.ElapsedMilliseconds} {res}");
    if (res[(int)Math.Truncate(res.Length / 4.0)] > 0x7F)
        return true;
    return false;
}
```

## Le joueur

- Un joueur doit être authentifié par login / mot de passe auprès du serveur d’identification :
    - Le serveur d’authentification doit retourner un token prouvant l’authentification.
    - Un joueur est composé d’un nom et d’un token d’authentification.

Le login via le serveur d’authentification est fait, et le token est bien sauvegardé avec le joueur.
Il n'est pas rafrachit et comme ce n'était pas précisé nous ne regarderons s'il l'utilisateur a acheter le jeu.
Il y a aussi un système de fallback si jamais l'authentification ne fonctionne pas avec l'api pour s'authentifier via Gauniv.GameServer.

Pour pointer correctement vers l'api il y a une variable à changer :
-> Gauniv.GameServer.Core.GameServer.cs
-> ligne 19
-> private string apiURL = "https://localhost:62966";

Vu que je n'ai pas trop touché à leurs parties, j'ai simplement rajouté Gauniv.Network comme dépendance.
Cela me permet d'utiliser la même méthode que Gauniv.Client pour communiquer avec le serveur d'authentification.

J'avais des erreurs de connection et je ne sais pas si c'est nécessaire de le faire mais j'avais aussi modifié les urls:
-> Gauniv.Network.OpenAPIs
-> v1.json et v11.json

    "servers": [
        {
            "url": "https://localhost:7209" // j'ai changé l'ip et le port par ce que me donnait docker, même valeur que pour apiURL
        },
        {
            "url": "http://localhost:5231"
        }
    ],

## Option

- Le serveur sait gérer plusieurs parties en même temps (et donc il sait gérer des salons). - Done!

- Lancer plusieurs serveurs en même temps pour augmenter la capacité maximale de joueurs :
  - Un joueur peut se connecter à n'importe quel serveur et jouer à n'importe quelle partie.
  - Si le serveur sait gérer plusieurs parties à la fois, alors le joueur peut choisir la partie à rejoindre quel que soit son serveur d'origine.

- Séparer la partie serveur de la partie jeu : - Commencé! (voir branche /feature/game/plugin)
  - Le serveur est générique et charge des plugins, chaque plugin est un jeu.
  - Le serveur peut gérer plusieurs jeux en même temps.
  - On peut rajouter un jeu sans redémarrer le serveur.
