using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickTix.Desktop.Services;
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

        // Cargar lista
        [RelayCommand]
        public virtual async Task LoadAsync()
        {
            try
            {
                var list = await _httpClient.GetListAsync<T>($"api/{Endpoint}");
                Items = new ObservableCollection<T>(list);
            }
            catch (ApiException apiEx)
            {
                MessageBox.Show(
                    $"Error cargando {Endpoint}.\nCódigo: {(int)apiEx.StatusCode}\nMensaje: {apiEx.Message}",
                    "Error API",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error local cargando {Endpoint}: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Añadir nuevo
        [RelayCommand]
        public virtual async Task AddAsync(TCreate newItem)
        {
            try
            {
                await _httpClient.PostAsync<TCreate, T>($"api/{Endpoint}", newItem);
                await LoadAsync();
            }
            catch (ApiException apiEx)
            {
                MessageBox.Show(
                    $"Error añadiendo {Endpoint}.\nCódigo: {(int)apiEx.StatusCode}\nMensaje: {apiEx.Message}",
                    "Error API",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error local añadiendo {Endpoint}: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Actualizar existente
        public virtual async Task UpdateAsync(int id, T updatedItem)
        {
            try
            {
                await _httpClient.PutAsync($"api/{Endpoint}/{id}", updatedItem);
                await LoadAsync();
            }
            catch (ApiException apiEx)
            {
                MessageBox.Show(
                    $"Error actualizando {Endpoint}.\nCódigo: {(int)apiEx.StatusCode}\nMensaje: {apiEx.Message}",
                    "Error API",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error local actualizando {Endpoint}: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Eliminar
        [RelayCommand]
        public virtual async Task DeleteAsync(int id)
        {
            if (id == 0)
                return;

            if (MessageBox.Show("¿Eliminar registro?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes)
                return;

            try
            {
                await _httpClient.DeleteAsync($"api/{Endpoint}/{id}");
                await LoadAsync();
            }
            catch (ApiException apiEx)
            {
                MessageBox.Show(
                    $"Error eliminando {Endpoint}.\nCódigo: {(int)apiEx.StatusCode}\nMensaje: {apiEx.Message}",
                    "Error API",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error local eliminando {Endpoint}: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        partial void OnSelectedItemChanged(T? value)
        {
            _ = HandleSelectedItemChangedAsync(value);
        }

        protected virtual Task OnSelectedItemChangedAsync(T? value)
        {
            return Task.CompletedTask;
        }

        private async Task HandleSelectedItemChangedAsync(T? value)
        {
            try
            {
                await OnSelectedItemChangedAsync(value);
            }
            catch (ApiException apiEx)
            {
                MessageBox.Show(
                    $"Error API al cambiar selección.\nCódigo: {(int)apiEx.StatusCode}\nMensaje: {apiEx.Message}",
                    "Error API",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error local al cambiar selección: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
