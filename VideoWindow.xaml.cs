using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using LibVLCSharp.Shared;

namespace Gullible;

public partial class VideoWindow
{
    private readonly bool _preventLogout;
    
    private DispatcherTimer _openWindowTimer;
    private DispatcherTimer _logoffTimer;
    
    private LibVLC _libVlc;
    
    public VideoWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var args = Environment.GetCommandLineArgs();
        _preventLogout = args.Contains("--no-logout");
    }
    
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        InitializeVlc();
        
        PlayEmbeddedVideo();
    }
    
    private void InitializeVlc()
    {
        var libVlcPath = Helpers.GetVlcPathFromRegistry();
        Core.Initialize(libVlcPath);
        _libVlc = new LibVLC();
        VideoView.MediaPlayer = new MediaPlayer(_libVlc);
        VideoView.MediaPlayer.Stopped += VideoPlayer_MediaEnded;
    }
    
    private void PlayEmbeddedVideo()
    {
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            var resource = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("Video.mp4"));

            using var videoStream = asm.GetManifestResourceStream(resource!);
            if (videoStream != null)
            {
                var tempFile = Path.Combine(Path.GetTempPath(), "tempVideo.mp4");
                using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
                {
                    videoStream.CopyTo(fileStream);
                }

                var media = new Media(_libVlc, tempFile);
                
                VideoView.MediaPlayer.Media = media;
                
                VideoView.MediaPlayer.Play();
                SetupOpenWindowTimer();
                SetupLogoffTimer();
            }
            else
            {
                MessageBox.Show("Embedded video not found!");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error playing video: {ex.Message}");
        }
    }
    
    private void SetupOpenWindowTimer()
    {
        _openWindowTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _openWindowTimer.Tick += OpenWindowTimer_Tick;
        _openWindowTimer.Start();
    }
    
    private void SetupLogoffTimer()
    {
        _logoffTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _logoffTimer.Tick += LogoffTimer_Tick;
        _logoffTimer.Start();
    }
    
    private void OpenWindowTimer_Tick(object sender, EventArgs e)
    {
        if (VideoView.MediaPlayer!.Position <  0.37f) return;
        _openWindowTimer.Stop();
        OpenTextWindow();
    }
    
    private void LogoffTimer_Tick(object sender, EventArgs e)
    {
        if (VideoView.MediaPlayer!.Position <  0.74f) return;
        _logoffTimer.Stop();
        if (!_preventLogout)
        {
            Helpers.ForceShutdown();
        }
    }

    private void OpenTextWindow()
    {
        var textWindow = new GullibleText
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        textWindow.Show();
        textWindow.Top = Top - textWindow.Height - 20;
        textWindow.Left = Left + (Width - textWindow.Width) / 2;
        textWindow.Activate();
    }
    
    private void VideoPlayer_MediaEnded(object sender, EventArgs eventArgs)
    {
        _logoffTimer.Stop();
        _openWindowTimer.Stop();
        Close();
    }
}