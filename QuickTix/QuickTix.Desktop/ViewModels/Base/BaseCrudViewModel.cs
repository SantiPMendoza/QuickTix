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

        // Nuevo: error persistente para mostrar en UI (flyouts)
        [ObservableProperty] private string? errorMessage;

        public BaseCrudViewModel(HttpJsonClient httpClient)
        {
            _httpClient = httpClient;
        }

        [RelayCommand]
        public virtual async Task LoadAsync()
        {
            try
            {
                ErrorMessage = null;
                var list = await _httpClient.GetListAsync<T>($"api/{Endpoint}");
                Items = new ObservableCollection<T>(list);
            }
            catch (ApiException apiEx)
            {
                ErrorMessage = $"Error cargando {Endpoint}.\nCódigo: {(int)apiEx.StatusCode}\nMensaje: {apiEx.Message}";
                MessageBox.Show(ErrorMessage, "Error API", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error local cargando {Endpoint}: {ex.Message}";
                MessageBox.Show(ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Nuevo: alta con resultado (sin MessageBox: el formulario decide)
        public virtual async Task<bool> TryAddAsync(TCreate newItem)
        {
            try
            {
                ErrorMessage = null;
                await _httpClient.PostAsync<TCreate, T>($"api/{Endpoint}", newItem);
                await LoadAsync();
                return true;
            }
            catch (ApiException apiEx)
            {
                ErrorMessage = $"Error añadiendo {Endpoint}.\nCódigo: {(int)apiEx.StatusCode}\nMensaje: {apiEx.Message}";
                return false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error local añadiendo {Endpoint}: {ex.Message}";
                return false;
            }
        }

        // Mantengo el comando existente para pantallas que no usan flyout inline
        [RelayCommand]
        public virtual async Task AddAsync(TCreate newItem)
        {
            var ok = await TryAddAsync(newItem);
            if (!ok && !string.IsNullOrWhiteSpace(ErrorMessage))
            {
                MessageBox.Show(ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Nuevo: update con resultado (sin MessageBox: el formulario decide)
        public virtual async Task<bool> TryUpdateAsync(int id, T updatedItem)
        {
            try
            {
                ErrorMessage = null;
                await _httpClient.PutAsync($"api/{Endpoint}/{id}", updatedItem);
                await LoadAsync();
                return true;
            }
            catch (ApiException apiEx)
            {
                ErrorMessage = $"Error actualizando {Endpoint}.\nCódigo: {(int)apiEx.StatusCode}\nMensaje: {apiEx.Message}";
                return false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error local actualizando {Endpoint}: {ex.Message}";
                return false;
            }
        }

        // Mantengo firma que ya usas (no command) por compatibilidad
        public virtual async Task UpdateAsync(int id, T updatedItem)
        {
            var ok = await TryUpdateAsync(id, updatedItem);
            if (!ok && !string.IsNullOrWhiteSpace(ErrorMessage))
            {
                MessageBox.Show(ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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
                ErrorMessage = null;
                await _httpClient.DeleteAsync($"api/{Endpoint}/{id}");
                await LoadAsync();
            }
            catch (ApiException apiEx)
            {
                ErrorMessage = $"Error eliminando {Endpoint}.\nCódigo: {(int)apiEx.StatusCode}\nMensaje: {apiEx.Message}";
                MessageBox.Show(ErrorMessage, "Error API", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error local eliminando {Endpoint}: {ex.Message}";
                MessageBox.Show(ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                ErrorMessage = null;
                await OnSelectedItemChangedAsync(value);
            }
            catch (ApiException apiEx)
            {
                ErrorMessage = $"Error API al cambiar selección.\nCódigo: {(int)apiEx.StatusCode}\nMensaje: {apiEx.Message}";
                MessageBox.Show(ErrorMessage, "Error API", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error local al cambiar selección: {ex.Message}";
                MessageBox.Show(ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
