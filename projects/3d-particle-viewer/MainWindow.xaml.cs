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
    private readonly Model3DGroup modelScene = new();
    private readonly Transform3DGroup modelTransform = new();
    private readonly ParticleSimulation simulation = new();
    private readonly System.Windows.Threading.DispatcherTimer simulationTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private ObjModel? loadedModel;
    private GeometryModel3D? originalMeshVisual;
    private GeometryModel3D? particleVisual;
    private List<Point3D> particlePoints = [];
    private BitmapSource? particleImage;
    private GeometryModel3D? groundVisual;
    private bool simulationIsActive;
    private Point lastMousePosition;
    private bool isDragging;
    private double cameraYaw = 0;
    private double cameraPitch = 12;
    private double cameraDistance = 7;

    public MainWindow()
    {
        InitializeComponent();
        SceneVisual.Content = scene;
        scene.Children.Add(modelScene);
        modelTransform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0)));
        modelTransform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 0)));
        modelScene.Transform = modelTransform;
        simulationTimer.Tick += SimulationTimer_Tick;
        UpdateCamera();
        UpdateSimulationLabels();
        UpdateGroundVisual();
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
            modelScene.Children.Clear();
            originalMeshVisual = null;
            particleVisual = null;
            simulation.Reset([]);
            StatusText.Text = error.Message;
            ViewportHint.Text = "Load an OBJ model to begin";
        }
    }

    private void RebuildVisualization()
    {
        if (loadedModel is null) return;
        modelScene.Children.Clear();
        var surface = ParticleGenerator.CreateSurfaceMesh(loadedModel);
        var surfaceMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(18, 170, 190, 220)));
        originalMeshVisual = new GeometryModel3D(surface, surfaceMaterial);
        particlePoints = ParticleGenerator.SampleSurface(loadedModel, (int)DensitySlider.Value);
        simulation.Reset(particlePoints);
        UpdateOriginalMeshVisibility();
        RebuildParticles();
    }

    private void RebuildParticles()
    {
        if (loadedModel is null) return;
        var count = (int)DensitySlider.Value;
        particlePoints = ParticleGenerator.SampleSurface(loadedModel, count);
        simulation.Reset(particlePoints);
        UpdateParticleVisual();
        DensityValue.Text = $"{count:N0} particles";
        SizeValue.Text = SizeSlider.Value.ToString("0.000", CultureInfo.InvariantCulture);
    }

    private void UpdateParticleVisual()
    {
        if (loadedModel is null) return;
        var shape = GetSelectedShape();
        if (particleVisual is not null) modelScene.Children.Remove(particleVisual);
        var visiblePoints = simulationIsActive ? simulation.Positions : particlePoints;
        var geometry = shape is ParticleShape.Billboard or ParticleShape.ImageBillboard
            ? ParticleGenerator.CreateBillboardMesh(visiblePoints, SizeSlider.Value, Camera.Position, RotationSlider.Value)
            : ParticleGenerator.CreateParticleMesh(visiblePoints, SizeSlider.Value, shape);
        particleVisual = new GeometryModel3D(geometry, CreateParticleMaterial(shape));
        modelScene.Children.Add(particleVisual);
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
            particleVisual.Geometry = ParticleGenerator.CreateBillboardMesh(GetVisibleParticlePoints(), SizeSlider.Value, Camera.Position, RotationSlider.Value);
    }

    private ParticleShape GetSelectedShape() => (ParticleShape)Math.Max(0, ParticleShapeComboBox.SelectedIndex);

    private IReadOnlyList<Point3D> GetVisibleParticlePoints() => simulationIsActive ? simulation.Positions : particlePoints;

    private void UpdateOriginalMeshVisibility()
    {
        if (originalMeshVisual is null) return;
        if (ShowOriginalMeshCheckBox.IsChecked == true)
        {
            if (!modelScene.Children.Contains(originalMeshVisual)) modelScene.Children.Insert(0, originalMeshVisual);
        }
        else modelScene.Children.Remove(originalMeshVisual);
    }

    private void SoftBody_Changed(object sender, RoutedEventArgs e)
    {
        simulationIsActive = SoftBodyCheckBox.IsChecked == true && loadedModel is not null;
        if (simulationIsActive)
        {
            simulationTimer.Start();
            StatusText.Text = "Soft body simulation active";
        }
        else
        {
            simulationTimer.Stop();
            ResetSimulationState();
            if (loadedModel is not null) UpdateParticleVisual();
            StatusText.Text = loadedModel is null ? "No model loaded" : "Static particle visualization";
        }
    }

    private void SimulationSettings_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdateSimulationLabels();

    private void UpdateSimulationLabels()
    {
        if (SimulationStrengthValue is null || DampingValue is null || TimeStepValue is null) return;
        SimulationStrengthValue.Text = SimulationStrengthSlider.Value.ToString("0.0", CultureInfo.InvariantCulture);
        DampingValue.Text = DampingSlider.Value.ToString("0.000", CultureInfo.InvariantCulture);
        TimeStepValue.Text = $"{TimeStepSlider.Value:0.000} s";
    }

    private void SimulationTimer_Tick(object? sender, EventArgs e)
    {
        if (!simulationIsActive || loadedModel is null) return;
        simulation.Step(TimeStepSlider.Value, SimulationStrengthSlider.Value, DampingSlider.Value, GroundPlaneCheckBox.IsChecked == true, GroundHeightSlider.Value, SizeSlider.Value / 2);
        var shape = GetSelectedShape();
        if (particleVisual is null) return;
        if (shape is ParticleShape.Billboard or ParticleShape.ImageBillboard)
            particleVisual.Geometry = ParticleGenerator.CreateBillboardMesh(simulation.Positions, SizeSlider.Value, Camera.Position, RotationSlider.Value);
        else if (!ParticleGenerator.UpdateParticleMesh((MeshGeometry3D)particleVisual.Geometry, simulation.Positions, SizeSlider.Value, shape))
            UpdateParticleVisual();
    }

    private void ResetSimulation_Click(object sender, RoutedEventArgs e)
    {
        ResetSimulationState();
        if (loadedModel is not null) UpdateParticleVisual();
        StatusText.Text = loadedModel is null ? "No model loaded" : "Simulation reset";
    }

    private void ResetSimulationState() => simulation.Reset(particlePoints);

    private void GroundPlane_Changed(object sender, RoutedEventArgs e) => UpdateGroundVisual();

    private void GroundHeight_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        GroundHeightValue.Text = GroundHeightSlider.Value.ToString("0.00", CultureInfo.InvariantCulture);
        UpdateGroundVisual();
    }

    private void UpdateGroundVisual()
    {
        if (groundVisual is not null) scene.Children.Remove(groundVisual);
        groundVisual = null;
        GroundHeightValue.Text = GroundHeightSlider.Value.ToString("0.00", CultureInfo.InvariantCulture);
        if (GroundPlaneCheckBox.IsChecked != true) return;
        var y = GroundHeightSlider.Value;
        var mesh = new MeshGeometry3D();
        mesh.Positions.Add(new Point3D(-6, y, -6)); mesh.Positions.Add(new Point3D(6, y, -6)); mesh.Positions.Add(new Point3D(6, y, 6)); mesh.Positions.Add(new Point3D(-6, y, 6));
        mesh.TriangleIndices.Add(0); mesh.TriangleIndices.Add(1); mesh.TriangleIndices.Add(2); mesh.TriangleIndices.Add(0); mesh.TriangleIndices.Add(2); mesh.TriangleIndices.Add(3);
        groundVisual = new GeometryModel3D(mesh, new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(45, 120, 140, 170))));
        scene.Children.Add(groundVisual);
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
            LoadParticleImage(dialog.FileName, Path.GetFileName(dialog.FileName));
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or ArgumentException)
        {
            particleImage = null;
            ParticleImageText.Text = $"Could not load image: {error.Message}";
        }
    }

    private void UseBuiltInImage_Click(object sender, RoutedEventArgs e)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "radial_gradient.png");
        try { LoadParticleImage(path, "radial_gradient.png (built-in)"); }
        catch (Exception error) when (error is IOException or InvalidOperationException or ArgumentException)
        {
            ParticleImageText.Text = $"Could not load built-in image: {error.Message}";
        }
    }

    private void LoadParticleImage(string path, string displayName)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(path, UriKind.Absolute);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();
        particleImage = bitmap;
        ParticleImageText.Text = displayName;
        if (loadedModel is not null) UpdateParticleVisual();
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
        SoftBodyCheckBox.IsChecked = false;
        GroundPlaneCheckBox.IsChecked = false;
        simulationIsActive = false;
        simulationTimer.Stop();
        ResetSimulationState();
        RotationSlider.Value = 0;
        ResetCamera_Click(sender, e);
        UpdateGroundVisual();
        if (loadedModel is not null) UpdateParticleVisual();
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
