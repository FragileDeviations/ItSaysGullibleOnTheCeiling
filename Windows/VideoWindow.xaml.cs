using System.IO;
using System.Windows;
using System.Windows.Threading;
using LibVLCSharp.Shared;

namespace Gullible.Windows;

public partial class VideoWindow
{
    #region Timers 
    
    private DispatcherTimer _openWindowTimer;
    private DispatcherTimer _logoffTimer;
    private DispatcherTimer _closeTimer;
    
    #endregion
    
    #region VLC
    
    private LibVLC _libVlc;
    private MediaPlayer _mediaPlayer;
    
    #endregion
    
    #region Command Line Arguments
    
    private readonly bool _preventLogout;
    
    #endregion
    
    public VideoWindow()
    {
        InitializeComponent();
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Loaded += MainWindow_Loaded;
        
        var args = Environment.GetCommandLineArgs();
        _preventLogout = args.Contains("--no-logout");
    }
    
    #region Events
    
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        InitializeVlc();
        
        PlayEmbeddedVideo();
    }
    
    private void VideoPlayer_MediaEnded(object sender, EventArgs eventArgs)
    {
        _logoffTimer.Stop();
        _openWindowTimer.Stop();
    }
    
    private void OpenWindowTimer_Tick(object sender, EventArgs e)
    {
        if (VideoView.MediaPlayer!.Position <  0.37f) return;
        _openWindowTimer.Stop();
        
        ShowGullibleText();
    }
    
    private void LogoffTimer_Tick(object sender, EventArgs e)
    {
        if (VideoView.MediaPlayer!.Position <  0.74f) return;
        _logoffTimer.Stop();
        if (!_preventLogout)
        {
            HelperMethods.ForceShutdown();
        }
    }
    
    private void CloseTimer_Tick(object sender, EventArgs e)
    {
        if (VideoView.MediaPlayer!.Position <  0.94f) return;
        _closeTimer.Stop();
        Close();
    }
    
    #endregion
    
    private void InitializeVlc()
    {
        var libVlcPath = HelperMethods.GetVlcPathFromRegistry();
        Core.Initialize(libVlcPath);
        _libVlc = new LibVLC();
        
        _mediaPlayer = new MediaPlayer(_libVlc);
        _mediaPlayer.Stopped += VideoPlayer_MediaEnded;
        VideoView.MediaPlayer = _mediaPlayer;
    }

    private void PlayEmbeddedVideo()
    {
        try
        {
            var resource = App.ProgramAsm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("Video.mp4"));

            using var videoStream = App.ProgramAsm.GetManifestResourceStream(resource!);
            if (videoStream != null)
            {
                var tempFile = Path.Combine(Path.GetTempPath(), "tempVideo.mp4");
                using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
                {
                    videoStream.CopyTo(fileStream);
                }

                var media = new Media(_libVlc, tempFile);

                VideoView.MediaPlayer!.Media = media;

                VideoView.MediaPlayer.Play();
                SetupTimers();
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

        return;

        void SetupTimers()
        {
            _openWindowTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _openWindowTimer.Tick += OpenWindowTimer_Tick;
            _openWindowTimer.Start();
            
            _logoffTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _logoffTimer.Tick += LogoffTimer_Tick;
            _logoffTimer.Start();
            
            _closeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _closeTimer.Tick += CloseTimer_Tick;
            _closeTimer.Start();
        }
    }

    private void ShowGullibleText()
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
}