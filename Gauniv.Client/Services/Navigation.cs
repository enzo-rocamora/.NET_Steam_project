using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Controls;

namespace Gauniv.Client.Services
{
    /// <summary>
    /// Aucune raison de toucher a quelque chose ici
    /// </summary>
    public partial class NavigationService() : ObservableObject
    {
        public static NavigationService Instance { get; private set; } = new NavigationService();
        public bool CanGoBack => App.Current?.MainPage?.Navigation.NavigationStack.Count > 0;

        [ObservableProperty]
        private ContentPage currentPage;

        public async void GoBack() => await App.Current.MainPage.Navigation.PopAsync();

        /// <summary>
        /// Permet de changer la page afficher par la <see cref="Frame"/>
        /// </summary>
        /// <typeparam name="T">Le type du ViewModel a afficher</typeparam>
        /// <param name="args">les paramètres pour instancier le ViewModel. /!\ votre ViewModel doit avoir un constructeur compatible avec vos paramètres</param>
        public async void Navigate<T>(Dictionary<string, object> args, bool clear = false) where T : ContentPage
        {
            Routing.RegisterRoute($"{typeof(T).Name}", typeof(T));
            string t = $"{typeof(T).Name}";
            if(clear)
            {
                await Shell.Current.Navigation.PopToRootAsync();
            }
            // Si vous avez une exception ici, cela veut dire que le constructeur de votre view model ne correspond pas au paramèetres que vous passez !
            await Shell.Current.GoToAsync($"{typeof(T).Name}", args);
        }

    }
}
