using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuickTix.Desktop.Controls
{
    /// <summary>
    /// Modal común del tema Vibra: Popup centrado con tarjeta elevada, título,
    /// botón de cierre opcional y áreas de contenido y botonera.
    /// Las vistas aportan <see cref="DialogContent"/> y <see cref="Footer"/>;
    /// el control no fija DataContext, así que los bindings del formulario
    /// se resuelven contra el DataContext heredado de la página.
    /// </summary>
    public partial class VibraDialog : UserControl
    {
        /// <summary>Identifica la propiedad de dependencia <see cref="IsOpen"/>.</summary>
        public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
            nameof(IsOpen),
            typeof(bool),
            typeof(VibraDialog),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>Identifica la propiedad de dependencia <see cref="Title"/>.</summary>
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(VibraDialog),
            new PropertyMetadata(string.Empty));

        /// <summary>Identifica la propiedad de dependencia <see cref="DialogContent"/>.</summary>
        public static readonly DependencyProperty DialogContentProperty = DependencyProperty.Register(
            nameof(DialogContent),
            typeof(object),
            typeof(VibraDialog),
            new PropertyMetadata(null));

        /// <summary>Identifica la propiedad de dependencia <see cref="Footer"/>.</summary>
        public static readonly DependencyProperty FooterProperty = DependencyProperty.Register(
            nameof(Footer),
            typeof(object),
            typeof(VibraDialog),
            new PropertyMetadata(null));

        /// <summary>Identifica la propiedad de dependencia <see cref="DialogMinWidth"/>.</summary>
        public static readonly DependencyProperty DialogMinWidthProperty = DependencyProperty.Register(
            nameof(DialogMinWidth),
            typeof(double),
            typeof(VibraDialog),
            new PropertyMetadata(400d));

        /// <summary>Identifica la propiedad de dependencia <see cref="CloseCommand"/>.</summary>
        public static readonly DependencyProperty CloseCommandProperty = DependencyProperty.Register(
            nameof(CloseCommand),
            typeof(ICommand),
            typeof(VibraDialog),
            new PropertyMetadata(null));

        /// <summary>
        /// Controla la visibilidad del modal (enlazable en dos direcciones al ViewModel).
        /// </summary>
        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        /// <summary>
        /// Título mostrado en la cabecera con la tipografía display de Vibra.
        /// </summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Contenido del cuerpo del modal (normalmente el formulario de la vista).
        /// </summary>
        public object? DialogContent
        {
            get => GetValue(DialogContentProperty);
            set => SetValue(DialogContentProperty, value);
        }

        /// <summary>
        /// Botonera inferior del modal (el chrome la alinea a la derecha).
        /// </summary>
        public object? Footer
        {
            get => GetValue(FooterProperty);
            set => SetValue(FooterProperty, value);
        }

        /// <summary>
        /// Ancho mínimo de la tarjeta del modal (400 por defecto).
        /// </summary>
        public double DialogMinWidth
        {
            get => (double)GetValue(DialogMinWidthProperty);
            set => SetValue(DialogMinWidthProperty, value);
        }

        /// <summary>
        /// Comando opcional de cierre: si se aporta, se muestra la "✕" en la cabecera.
        /// </summary>
        public ICommand? CloseCommand
        {
            get => (ICommand?)GetValue(CloseCommandProperty);
            set => SetValue(CloseCommandProperty, value);
        }

        /// <summary>
        /// Inicializa el componente modal.
        /// </summary>
        public VibraDialog()
        {
            InitializeComponent();
        }
    }
}
