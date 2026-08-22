using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace BounceSimulator;

public partial class MainWindow : Window
{
    private readonly Model3DGroup scene = new();
    private ImportedModel? loadedModel;
    private GeometryModel3D? modelVisual;
    private Point lastMousePosition;
    private bool isOrbiting;
    private double cameraYaw;
    private double cameraPitch = 12;
    private double cameraDistance = 7;
    private double defaultCameraDistance = 7;

    public MainWindow()
    {
        InitializeComponent();
        ModelVisual.Content = scene;
        ResetCameraState();
    }

    private void LoadModel_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "3D Models (*.obj;*.fbx)|*.obj;*.fbx|OBJ (*.obj)|*.obj|FBX (*.fbx)|*.fbx|All Files (*.*)|*.*"
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var model = ModelImporter.Load(dialog.FileName);
            loadedModel = model;
            var material = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(145, 168, 210)));
            modelVisual = new GeometryModel3D(model.Geometry, material) { BackMaterial = material };
            scene.Children.Clear();
            scene.Children.Add(modelVisual);
            defaultCameraDistance = Math.Clamp(model.Radius * 2.6, 4.5, 16);
            ResetCameraState();
            StatusText.Text = $"{Path.GetFileName(dialog.FileName)} • {model.VertexCount:N0} vertices • {model.TriangleCount:N0} triangles";
            ViewportHint.Text = "Drag to orbit the model";
        }
        catch (Exception error)
        {
            loadedModel = null;
            modelVisual = null;
            scene.Children.Clear();
            StatusText.Text = error.Message;
            ViewportHint.Text = "Load an OBJ or FBX model to begin";
            System.Diagnostics.Debug.WriteLine($"Model import failed: {error}");
        }
    }

    private void ResetView_Click(object sender, RoutedEventArgs e) => ResetCameraState();

    private void Viewport_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        isOrbiting = true;
        lastMousePosition = e.GetPosition(Viewport);
        Viewport.CaptureMouse();
        e.Handled = true;
    }

    private void Viewport_MouseMove(object sender, MouseEventArgs e)
    {
        if (!isOrbiting) return;
        var position = e.GetPosition(Viewport);
        cameraYaw -= (position.X - lastMousePosition.X) * 0.5;
        cameraPitch = Math.Clamp(cameraPitch + (position.Y - lastMousePosition.Y) * 0.5, -80, 80);
        lastMousePosition = position;
        UpdateCamera();
    }

    private void Viewport_MouseUp(object sender, MouseButtonEventArgs e) => EndOrbit();

    private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        cameraDistance = Math.Clamp(cameraDistance - e.Delta * 0.002, Math.Max(1.5, defaultCameraDistance * 0.25), defaultCameraDistance * 3.5);
        UpdateCamera();
    }

    private void Window_Deactivated(object sender, EventArgs e) => EndOrbit();
    private void Window_Closed(object? sender, EventArgs e) => EndOrbit();

    private void EndOrbit()
    {
        isOrbiting = false;
        if (Viewport.IsMouseCaptured) Viewport.ReleaseMouseCapture();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.R) ResetCameraState();
        base.OnKeyDown(e);
    }

    private void ResetCameraState()
    {
        cameraYaw = 0;
        cameraPitch = 12;
        cameraDistance = defaultCameraDistance;
        UpdateCamera();
    }

    private void UpdateCamera()
    {
        var yaw = cameraYaw * Math.PI / 180;
        var pitch = cameraPitch * Math.PI / 180;
        var position = new Point3D(cameraDistance * Math.Cos(pitch) * Math.Sin(yaw), cameraDistance * Math.Sin(pitch), cameraDistance * Math.Cos(pitch) * Math.Cos(yaw));
        Camera.Position = position;
        Camera.LookDirection = new Vector3D(-position.X, -position.Y, -position.Z);
        Camera.UpDirection = new Vector3D(0, 1, 0);
    }
}
