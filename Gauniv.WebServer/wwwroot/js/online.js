// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
"use strict";

// Initialiser la connexion SignalR
var connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/online")
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000]) // Tentatives de reconnexion avec délais croissants
    .build();

// Écouter les changements de statut des utilisateurs
connection.on("UserStatusChanged", function (data) {
    console.log("Status changed:", data);
    // Vous pouvez émettre un événement personnalisé pour que d'autres scripts puissent réagir
    document.dispatchEvent(new CustomEvent('userStatusChanged', { detail: data }));
});

// Écouter les mises à jour des compteurs de joueurs
connection.on("PlayerCountsUpdated", function (data) {
    console.log("Player counts updated:", data);
    // Émettre un événement pour les mises à jour des compteurs
    document.dispatchEvent(new CustomEvent('playerCountsUpdated', { detail: data }));
});

// Fonction pour démarrer la connexion
function startConnection() {
    connection.start()
        .then(function () {
            console.log("Connected to SignalR hub");
            // Vous pouvez émettre un événement de connexion réussie
            document.dispatchEvent(new CustomEvent('signalRConnected'));
        })
        .catch(function (err) {
            console.error("Error connecting to SignalR:", err.toString());
            // Réessayer dans 5 secondes
            setTimeout(startConnection, 5000);
        });
}

// Gérer la déconnexion
connection.onclose(async (error) => {
    console.log("SignalR disconnected:", error);
    // Tentative de reconnexion automatique (géré par withAutomaticReconnect)
});

// Démarrer la connexion initiale
startConnection();

// Exposer la connexion globalement pour pouvoir l'utiliser dans d'autres scripts
window.gameHub = {
    connection: connection,

    // Méthode pour démarrer un jeu
    startGame: async function (gameId) {
        try {
            await connection.invoke("UpdateGameStatus", gameId);
            console.log(`Started game ${gameId}`);
            return true;
        } catch (err) {
            console.error("Error starting game:", err);
            return false;
        }
    },

    // Méthode pour arrêter un jeu
    stopGame: async function () {
        try {
            await connection.invoke("UpdateGameStatus", null);
            console.log("Stopped game");
            return true;
        } catch (err) {
            console.error("Error stopping game:", err);
            return false;
        }
    }
};

// Écouter l'événement 'beforeunload' pour nettoyer la connexion si nécessaire
window.addEventListener('beforeunload', function () {
    if (connection && connection.state === signalR.HubConnectionState.Connected) {
        // Arrêter le jeu si l'utilisateur quitte la page
        window.gameHub.stopGame().catch(console.error);
    }
});