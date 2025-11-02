using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;

namespace QuickTix.Desktop.ViewModels.Base
{
    public abstract partial class BaseCrudViewModel<T, TCreate> : ObservableObject
        where T : class
        where TCreate : class, new()
    {
        protected readonly HttpJsonClient _httpClient;
        protected abstract string Endpoint { get; }

        [ObservableProperty] private ObservableCollection<T> items = [];
        [ObservableProperty] private T? selectedItem;

        public BaseCrudViewModel(HttpJsonClient httpClient)
        {
            _httpClient = httpClient;
        }

        // 🔹 Cargar lista
        [RelayCommand]
        public virtual async Task LoadAsync()
        {
            try
            {
                var list = await _httpClient.GetListAsync<T>($"api/{Endpoint}");
                Items = new ObservableCollection<T>(list);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading {Endpoint}: {ex.Message}");
            }
        }

        // 🔹 Añadir nuevo
        [RelayCommand]
        public virtual async Task AddAsync(TCreate newItem)
        {
            try
            {
                await _httpClient.PostAsync<TCreate, T>($"api/{Endpoint}", newItem);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding {Endpoint}: {ex.Message}");
            }
        }

        // 🔹 Actualizar existente
        public virtual async Task UpdateAsync(int id, TCreate updatedItem)
        {
            try
            {
                await _httpClient.PutAsync($"api/{Endpoint}/{id}", updatedItem);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating {Endpoint}: {ex.Message}");
            }
        }

        // 🔹 Eliminar
        [RelayCommand]
        public virtual async Task DeleteAsync(int id)
        {
            if (id == 0) return;

            if (MessageBox.Show("¿Eliminar registro?", "Confirmar", MessageBoxButton.YesNo)
                != MessageBoxResult.Yes)
                return;

            try
            {
                await _httpClient.DeleteAsync($"api/{Endpoint}/{id}");
                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting {Endpoint}: {ex.Message}");
            }
        }
    }
}
