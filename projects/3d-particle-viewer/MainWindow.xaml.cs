using Microsoft.Win32;
using System;
using System.Globalization;
using System.Windows;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;

namespace ParticleModelViewer;

public partial class MainWindow : Window
{
    private readonly Model3DGroup scene = new();
    private readonly Transform3DGroup modelTransform = new();
    private ObjModel? loadedModel;
    private GeometryModel3D? originalMeshVisual;
    private GeometryModel3D? particleVisual;
    private List<Point3D> particlePoints = [];
    private BitmapSource? particleImage;
    private Point lastMousePosition;
    private bool isDragging;
    private double cameraYaw = 0;
    private double cameraPitch = 12;
    private double cameraDistance = 7;

    public MainWindow()
    {
        InitializeComponent();
        SceneVisual.Content = scene;
        modelTransform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0)));
        modelTransform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 0)));
        scene.Transform = modelTransform;
        UpdateCamera();
    }

    private void LoadModel_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "3D models (*.obj;*.fbx)|*.obj;*.fbx|OBJ files (*.obj)|*.obj|FBX files (*.fbx)|*.fbx" };
        if (dialog.ShowDialog() != true) return;
        StatusText.Text = "Loading model…";
        try
        {
            if (Path.GetExtension(dialog.FileName).Equals(".fbx", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException("FBX loading is planned for a later phase. Please choose an OBJ file.");
            loadedModel = Normalize(ObjModel.Load(dialog.FileName));
            ViewportHint.Text = "Drag to orbit the particle surface";
            StatusText.Text = $"{Path.GetFileName(dialog.FileName)} • {loadedModel.Vertices.Count:N0} vertices • {loadedModel.Triangles.Count:N0} triangles";
            RebuildVisualization();
            ResetCamera_Click(this, new RoutedEventArgs());
        }
        catch (Exception error) when (error is IOException or InvalidDataException or FormatException or NotSupportedException)
        {
            loadedModel = null;
            scene.Children.Clear();
            StatusText.Text = error.Message;
            ViewportHint.Text = "Load an OBJ model to begin";
        }
    }

    private void RebuildVisualization()
    {
        if (loadedModel is null) return;
        scene.Children.Clear();
        var surface = ParticleGenerator.CreateSurfaceMesh(loadedModel);
        var surfaceMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(18, 170, 190, 220)));
        originalMeshVisual = new GeometryModel3D(surface, surfaceMaterial);
        particlePoints = ParticleGenerator.SampleSurface(loadedModel, (int)DensitySlider.Value);
        UpdateOriginalMeshVisibility();
        RebuildParticles();
    }

    private void RebuildParticles()
    {
        if (loadedModel is null) return;
        var count = (int)DensitySlider.Value;
        particlePoints = ParticleGenerator.SampleSurface(loadedModel, count);
        UpdateParticleVisual();
        DensityValue.Text = $"{count:N0} particles";
        SizeValue.Text = SizeSlider.Value.ToString("0.000", CultureInfo.InvariantCulture);
    }

    private void UpdateParticleVisual()
    {
        if (loadedModel is null) return;
        var shape = GetSelectedShape();
        if (particleVisual is not null) scene.Children.Remove(particleVisual);
        var geometry = shape is ParticleShape.Billboard or ParticleShape.ImageBillboard
            ? ParticleGenerator.CreateBillboardMesh(particlePoints, SizeSlider.Value, Camera.Position, RotationSlider.Value)
            : ParticleGenerator.CreateParticleMesh(particlePoints, SizeSlider.Value, shape);
        particleVisual = new GeometryModel3D(geometry, CreateParticleMaterial(shape));
        scene.Children.Add(particleVisual);
    }

    private Material CreateParticleMaterial(ParticleShape shape)
    {
        if (shape == ParticleShape.ImageBillboard && particleImage is not null)
        {
            var imageBrush = new ImageBrush(particleImage) { Stretch = Stretch.Uniform };
            return new DiffuseMaterial(imageBrush);
        }
        try { return new DiffuseMaterial(new SolidColorBrush(ParseColor())); }
        catch (FormatException)
        {
            StatusText.Text = "Use a color such as #818CF8.";
            return new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(129, 140, 248)));
        }
    }

    private void UpdateBillboardGeometry()
    {
        if (particleVisual is null || loadedModel is null) return;
        var shape = GetSelectedShape();
        if (shape is ParticleShape.Billboard or ParticleShape.ImageBillboard)
            particleVisual.Geometry = ParticleGenerator.CreateBillboardMesh(particlePoints, SizeSlider.Value, Camera.Position, RotationSlider.Value);
    }

    private ParticleShape GetSelectedShape() => (ParticleShape)Math.Max(0, ParticleShapeComboBox.SelectedIndex);

    private void UpdateOriginalMeshVisibility()
    {
        if (originalMeshVisual is null) return;
        if (ShowOriginalMeshCheckBox.IsChecked == true)
        {
            if (!scene.Children.Contains(originalMeshVisual)) scene.Children.Insert(0, originalMeshVisual);
        }
        else scene.Children.Remove(originalMeshVisual);
    }

    private void ParticleSettings_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsInitialized || loadedModel is null) return;
        if (sender == DensitySlider) RebuildParticles();
        else
        {
            UpdateParticleVisual();
            SizeValue.Text = SizeSlider.Value.ToString("0.000", CultureInfo.InvariantCulture);
        }
    }

    private void ApplyColor_Click(object sender, RoutedEventArgs e)
    {
        if (loadedModel is not null) UpdateParticleVisual();
    }

    private void ParticleShape_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!IsInitialized) return;
        var isImage = GetSelectedShape() == ParticleShape.ImageBillboard;
        ImageControls.Visibility = isImage ? Visibility.Visible : Visibility.Collapsed;
        if (isImage && particleImage is null) ParticleImageText.Text = "No image selected; using a plain billboard.";
        if (loadedModel is not null) UpdateParticleVisual();
    }

    private void ShowOriginalMesh_Changed(object sender, RoutedEventArgs e) => UpdateOriginalMeshVisibility();

    private void LoadParticleImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Particle images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|PNG images (*.png)|*.png|JPEG images (*.jpg;*.jpeg)|*.jpg;*.jpeg" };
        if (dialog.ShowDialog() != true) return;
        var extension = Path.GetExtension(dialog.FileName).ToLowerInvariant();
        if (extension is not ".png" and not ".jpg" and not ".jpeg")
        {
            ParticleImageText.Text = "Unsupported image format. Choose PNG or JPG.";
            return;
        }
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(dialog.FileName, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            particleImage = bitmap;
            ParticleImageText.Text = Path.GetFileName(dialog.FileName);
            if (loadedModel is not null) UpdateParticleVisual();
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or ArgumentException)
        {
            particleImage = null;
            ParticleImageText.Text = $"Could not load image: {error.Message}";
        }
    }

    private Color ParseColor()
    {
        var value = ColorTextBox.Text.Trim();
        if (!value.StartsWith('#') || (value.Length != 7 && value.Length != 9)) throw new FormatException();
        return (Color)ColorConverter.ConvertFromString(value)!;
    }

    private void RotationSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsInitialized) return;
        ((AxisAngleRotation3D)((RotateTransform3D)modelTransform.Children[0]).Rotation).Angle = RotationSlider.Value;
        RotationValue.Text = $"{RotationSlider.Value:0}°";
        UpdateBillboardGeometry();
    }

    private void ResetCamera_Click(object sender, RoutedEventArgs e)
    {
        cameraYaw = 0;
        cameraPitch = 12;
        cameraDistance = GetDefaultCameraDistance();
        UpdateCamera();
    }

    private void ResetVisualization_Click(object sender, RoutedEventArgs e)
    {
        ColorTextBox.Text = "#818CF8"; DensitySlider.Value = 1600; SizeSlider.Value = 0.018; RotationSlider.Value = 0;
        ParticleShapeComboBox.SelectedIndex = 0;
        particleImage = null;
        ParticleImageText.Text = "No image selected";
        ResetCamera_Click(sender, e);
        if (loadedModel is not null) RebuildVisualization();
    }

    private void Viewport_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) { isDragging = true; lastMousePosition = e.GetPosition(Viewport); Viewport.CaptureMouse(); }
    }

    private void Viewport_MouseMove(object sender, MouseEventArgs e)
    {
        if (!isDragging) return;
        var position = e.GetPosition(Viewport);
        cameraYaw -= (position.X - lastMousePosition.X) * 0.5;
        cameraPitch = Math.Clamp(cameraPitch + (position.Y - lastMousePosition.Y) * 0.5, -80, 80);
        lastMousePosition = position;
        UpdateCamera();
    }

    private void Viewport_MouseUp(object sender, MouseButtonEventArgs e) { isDragging = false; Viewport.ReleaseMouseCapture(); }

    private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        cameraDistance = Math.Clamp(cameraDistance - e.Delta * 0.002, 2.5, 18);
        UpdateCamera();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.R) ResetCamera_Click(this, new RoutedEventArgs());
        base.OnKeyDown(e);
    }

    private void UpdateCamera()
    {
        var yaw = cameraYaw * Math.PI / 180; var pitch = cameraPitch * Math.PI / 180;
        var position = new Point3D(cameraDistance * Math.Cos(pitch) * Math.Sin(yaw), cameraDistance * Math.Sin(pitch), cameraDistance * Math.Cos(pitch) * Math.Cos(yaw));
        Camera.Position = position;
        Camera.LookDirection = new Vector3D(-position.X, -position.Y, -position.Z);
        Camera.UpDirection = new Vector3D(0, 1, 0);
        UpdateBillboardGeometry();
    }

    private double GetDefaultCameraDistance()
    {
        if (loadedModel is null) return 7;
        var radius = loadedModel.Vertices.Max(point => (point - new Point3D()).Length);
        return Math.Clamp(radius * 2.4, 5.5, 12);
    }

    private static ObjModel Normalize(ObjModel original)
    {
        var minX = original.Vertices.Min(point => point.X); var maxX = original.Vertices.Max(point => point.X);
        var minY = original.Vertices.Min(point => point.Y); var maxY = original.Vertices.Max(point => point.Y);
        var minZ = original.Vertices.Min(point => point.Z); var maxZ = original.Vertices.Max(point => point.Z);
        var center = new Point3D((minX + maxX) / 2, (minY + maxY) / 2, (minZ + maxZ) / 2);
        var largestDimension = Math.Max(maxX - minX, Math.Max(maxY - minY, maxZ - minZ));
        if (largestDimension <= 0) throw new InvalidDataException("The model has no measurable size.");
        var scale = 4 / largestDimension;
        var result = new ObjModel();
        result.Vertices.AddRange(original.Vertices.Select(point => new Point3D((point.X - center.X) * scale, (point.Y - center.Y) * scale, (point.Z - center.Z) * scale)));
        result.Triangles.AddRange(original.Triangles);
        return result;
    }
}
