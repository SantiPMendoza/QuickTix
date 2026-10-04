

namespace QuickTix.Desktop.ViewModels.Base
{
    /// <summary>
    /// ViewModel base genérico para operaciones CRUD contra la API.
    /// Centraliza la carga de datos, operaciones de alta, actualización y borrado,
    /// así como la gestión de selección y propagación de errores hacia la UI.
    /// </summary>
    /// <typeparam name="T">Tipo del elemento mostrado en el listado.</typeparam>
    /// <typeparam name="TCreate">Tipo del DTO usado para la creación del elemento.</typeparam>
    public abstract partial class BaseCrudViewModel<T, TCreate> : ObservableObject
        where T : class
        where TCreate : class, new()
    {
        /// <summary>
        /// Cliente HTTP encargado de consumir la API y procesar ApiResponse.
        /// </summary>
        protected readonly HttpJsonClient _httpClient;

        /// <summary>
        /// Nombre lógico del recurso (p.ej. "Venue", "Ticket").
        /// Se utiliza como base para construir rutas CRUD estándar.
        /// </summary>
        protected abstract string Endpoint { get; }

        /// <summary>
        /// Colección observable con los elementos cargados desde la API.
        /// </summary>
        [ObservableProperty] private ObservableCollection<T> items = [];

        /// <summary>
        /// Elemento actualmente seleccionado en la UI.
        /// </summary>
        [ObservableProperty] private T? selectedItem;

        // Mensaje de error persistente para UI (flyouts, banners, formularios inline, etc.)
        [ObservableProperty] private string? errorMessage;

        // ===== Aviso modal (VibraDialog en modo alerta, fix 2b) =====
        // Sustituye a los MessageBox: cada vista aloja un VibraDialog
        // enlazado a IsAlertOpen/AlertTitle/AlertMessage + CloseAlertCommand.

        /// <summary>Controla la visibilidad del aviso modal.</summary>
        [ObservableProperty] private bool isAlertOpen;

        /// <summary>Título del aviso modal.</summary>
        [ObservableProperty] private string? alertTitle;

        /// <summary>Mensaje del aviso modal.</summary>
        [ObservableProperty] private string? alertMessage;

        /// <summary>
        /// Muestra un aviso modal (VibraDialog) con título y mensaje.
        /// </summary>
        /// <param name="title">Título del aviso.</param>
        /// <param name="message">Mensaje del aviso.</param>
        protected void ShowAlert(string title, string message)
        {
            AlertTitle = title;
            AlertMessage = message;
            IsAlertOpen = true;
        }

        /// <summary>
        /// Cierra el aviso modal.
        /// </summary>
        [RelayCommand]
        private void CloseAlert() => IsAlertOpen = false;

        // ===== Confirmación de borrado (VibraDialog, fix 2b) =====

        /// <summary>Controla la visibilidad del diálogo de confirmación de borrado.</summary>
        [ObservableProperty] private bool isConfirmDeleteOpen;

        /// <summary>
        /// Snapshot del elemento a borrar, capturado al abrir el diálogo.
        /// El texto del diálogo debe bindear AQUÍ y no a SelectedItem: el popup
        /// no es modal y la selección puede cambiar con el diálogo abierto
        /// (se mostraría un nombre y se borraría otro).
        /// </summary>
        [ObservableProperty] private T? pendingDeleteItem;

        // Id pendiente de borrar mientras el diálogo de confirmación está abierto
        private int _pendingDeleteId;

        /// <summary>
        /// Ruta del endpoint de listado (GET).
        /// Por defecto apunta al CRUD base del recurso.
        /// </summary>
        /// <remarks>
        /// Debe sobrescribirse en ViewModels que consuman endpoints
        /// de solo lectura o históricos.
        /// </remarks>
        protected virtual string ListRoute => ApiRoutes.Crud.Base(Endpoint);

        /// <summary>
        /// Ruta del endpoint de creación (POST).
        /// Por defecto apunta al CRUD base del recurso.
        /// </summary>
        protected virtual string CreateRoute => ApiRoutes.Crud.Base(Endpoint);

        /// <summary>
        /// Construye la ruta del endpoint de actualización (PUT) para un recurso concreto.
        /// </summary>
        /// <param name="id">Identificador del recurso.</param>
        /// <returns>Ruta del endpoint de actualización.</returns>
        protected virtual string UpdateRoute(int id) => ApiRoutes.Crud.ById(Endpoint, id);

        /// <summary>
        /// Construye la ruta del endpoint de borrado (DELETE) para un recurso concreto.
        /// </summary>
        /// <param name="id">Identificador del recurso.</param>
        /// <returns>Ruta del endpoint de borrado.</returns>
        protected virtual string DeleteRoute(int id) => ApiRoutes.Crud.ById(Endpoint, id);

        /// <summary>
        /// Inicializa una nueva instancia del <see cref="BaseCrudViewModel{T, TCreate}"/>.
        /// </summary>
        /// <param name="httpClient">Cliente HTTP para consumo de la API.</param>
        protected BaseCrudViewModel(HttpJsonClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Carga el listado de elementos desde la API y actualiza la colección observable.
        /// </summary>
        /// <remarks>
        /// Este método es normalmente invocado al inicializar la vista
        /// o tras operaciones de alta, actualización o borrado.
        /// </remarks>
        /// <returns>Tarea asíncrona.</returns>
        [RelayCommand]
        public virtual async Task LoadAsync()
        {
            try
            {
                ErrorMessage = null;

                var list = await _httpClient.GetListAsync<T>(ListRoute);
                Items = new ObservableCollection<T>(list);
            }
            catch (ApiException apiEx)
            {
                ErrorMessage =
                    $"Error cargando {Endpoint}.\nCódigo: {(int)apiEx.StatusCode}\nMensaje: {apiEx.Message}";

                ShowAlert("Error API", ErrorMessage);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error local cargando {Endpoint}: {ex.Message}";
                ShowAlert("Error", ErrorMessage);
            }
        }

        /// <summary>
        /// Intenta crear un nuevo elemento en la API.
        /// No muestra mensajes de UI; devuelve el resultado de la operación.
        /// </summary>
        /// <param name="newItem">DTO con los datos de creación.</param>
        /// <returns>
        /// True si la operación se completó correctamente;
        /// false si se produjo un error controlado.
        /// </returns>
        public virtual async Task<bool> TryAddAsync(TCreate newItem)
        {
            try
            {
                ErrorMessage = null;

                await _httpClient.PostAsync<TCreate, T>(CreateRoute, newItem);
                await LoadAsync();

                return true;
            }
            catch (ApiException apiEx)
            {
                ErrorMessage =
                    $"Error añadiendo {Endpoint}.\nCódigo: {(int)apiEx.StatusCode}\nMensaje: {apiEx.Message}";
                return false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error local añadiendo {Endpoint}: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Crea un nuevo elemento mostrando un aviso modal en caso de error.
        /// </summary>
        /// <param name="newItem">DTO con los datos de creación.</param>
        /// <returns>Tarea asíncrona.</returns>
        [RelayCommand]
        public virtual async Task AddAsync(TCreate newItem)
        {
            var ok = await TryAddAsync(newItem);

            if (!ok && !string.IsNullOrWhiteSpace(ErrorMessage))
                ShowAlert("Error", ErrorMessage);
        }

        /// <summary>
        /// Intenta actualizar un elemento existente en la API.
        /// No muestra mensajes de UI; devuelve el resultado de la operación.
        /// </summary>
        /// <param name="id">Identificador del recurso a actualizar.</param>
        /// <param name="updatedItem">DTO con los datos actualizados.</param>
        /// <returns>
        /// True si la operación se completó correctamente;
        /// false si se produjo un error controlado.
        /// </returns>
        public virtual async Task<bool> TryUpdateAsync(int id, T updatedItem)
        {
            try
            {
                ErrorMessage = null;

                await _httpClient.PutAsync(UpdateRoute(id), updatedItem);
                await LoadAsync();

                return true;
            }
            catch (ApiException apiEx)
            {
                ErrorMessage =
                    $"Error actualizando {Endpoint}.\nCódigo: {(int)apiEx.StatusCode}\nMensaje: {apiEx.Message}";
                return false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error local actualizando {Endpoint}: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Actualiza un elemento mostrando un aviso modal en caso de error.
        /// </summary>
        /// <param name="id">Identificador del recurso.</param>
        /// <param name="updatedItem">DTO con los datos actualizados.</param>
        /// <returns>Tarea asíncrona.</returns>
        public virtual async Task UpdateAsync(int id, T updatedItem)
        {
            var ok = await TryUpdateAsync(id, updatedItem);

            if (!ok && !string.IsNullOrWhiteSpace(ErrorMessage))
                ShowAlert("Error", ErrorMessage);
        }

        /// <summary>
        /// Solicita la eliminación de un elemento abriendo el diálogo de
        /// confirmación (VibraDialog). El borrado real se ejecuta en
        /// <see cref="ConfirmDeleteCommand"/>; las vistas conservan su
        /// binding a DeleteCommand.
        /// </summary>
        /// <param name="id">Identificador del recurso a eliminar.</param>
        /// <returns>Tarea asíncrona.</returns>
        [RelayCommand]
        public virtual Task DeleteAsync(int id)
        {
            if (id == 0)
                return Task.CompletedTask;

            _pendingDeleteId = id;
            PendingDeleteItem = SelectedItem;
            IsConfirmDeleteOpen = true;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Confirma el borrado pendiente y lo ejecuta.
        /// </summary>
        /// <returns>Tarea asíncrona.</returns>
        [RelayCommand]
        private async Task ConfirmDelete()
        {
            var id = _pendingDeleteId;

            IsConfirmDeleteOpen = false;
            _pendingDeleteId = 0;
            PendingDeleteItem = null;

            if (id == 0)
                return;

            await ExecuteDeleteAsync(id);
        }

        /// <summary>
        /// Cierra el diálogo de confirmación sin eliminar nada.
        /// </summary>
        [RelayCommand]
        private void CancelDelete()
        {
            IsConfirmDeleteOpen = false;
            _pendingDeleteId = 0;
            PendingDeleteItem = null;
        }

        /// <summary>
        /// Ejecuta el borrado contra la API y recarga el listado.
        /// Muestra un aviso modal en caso de error.
        /// </summary>
        /// <param name="id">Identificador del recurso a eliminar.</param>
        /// <returns>Tarea asíncrona.</returns>
        protected virtual async Task ExecuteDeleteAsync(int id)
        {
            try
            {
                ErrorMessage = null;

                await _httpClient.DeleteAsync(DeleteRoute(id));
                await LoadAsync();
            }
            catch (ApiException apiEx)
            {
                ErrorMessage =
                    $"Error eliminando {Endpoint}.\nCódigo: {(int)apiEx.StatusCode}\nMensaje: {apiEx.Message}";

                ShowAlert("Error API", ErrorMessage);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error local eliminando {Endpoint}: {ex.Message}";
                ShowAlert("Error", ErrorMessage);
            }
        }

        /// <summary>
        /// Hook generado por CommunityToolkit cuando cambia la propiedad SelectedItem.
        /// </summary>
        /// <param name="value">Nuevo elemento seleccionado.</param>
        partial void OnSelectedItemChanged(T? value)
        {
            _ = HandleSelectedItemChangedAsync(value);
        }

        /// <summary>
        /// Punto de extensión para reaccionar a cambios de selección.
        /// </summary>
        /// <param name="value">Elemento seleccionado.</param>
        /// <returns>Tarea asíncrona.</returns>
        protected virtual Task OnSelectedItemChangedAsync(T? value)
            => Task.CompletedTask;

        /// <summary>
        /// Envuelve la lógica de cambio de selección con control de errores
        /// y propagación del mensaje a la UI.
        /// </summary>
        /// <param name="value">Elemento seleccionado.</param>
        /// <returns>Tarea asíncrona.</returns>
        private async Task HandleSelectedItemChangedAsync(T? value)
        {
            try
            {
                ErrorMessage = null;
                await OnSelectedItemChangedAsync(value);
            }
            catch (ApiException apiEx)
            {
                ErrorMessage =
                    $"Error API al cambiar selección.\nCódigo: {(int)apiEx.StatusCode}\nMensaje: {apiEx.Message}";

                ShowAlert("Error API", ErrorMessage);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error local al cambiar selección: {ex.Message}";
                ShowAlert("Error", ErrorMessage);
            }
        }
    }
}
