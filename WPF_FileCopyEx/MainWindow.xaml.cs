using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
namespace WPF_FileCopyEx
{
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private bool _Stop = false;

		/// <summary>
		/// コピーを開始します
		/// </summary>
		private void btnCopyStart_Click( object sender, RoutedEventArgs e )
		{
			//プログレスバー初期化
			this.progressBar1.Maximum = 100;
			this.progressBar1.Value = 0;

			//コピー処理実施
			PInvoke.Win32API copyObject = new PInvoke.Win32API();
			copyObject.ProgressChanged += new PInvoke.Win32API.CopyProgressEventHandler( copyObject_ProgressChanged );
			PInvoke.Win32API.ResultStatus ret = copyObject.CopyStart( txtSource.Text, txtDestination.Text, true );

			//結果判定
			switch( ret )
			{
				case PInvoke.Win32API.ResultStatus.Completed:
					MessageBox.Show("成功");
					break;
				case PInvoke.Win32API.ResultStatus.Stoped:
					MessageBox.Show( "中断" );
					break;
				case PInvoke.Win32API.ResultStatus.Failed:
					MessageBox.Show( "失敗" );
					break;
			}
		}
		/// <summary>
		///  コピーの進捗状況通知イベント
		/// </summary>
		void copyObject_ProgressChanged( object s, PInvoke.Win32API.CopyProgressEventArgs e )
		{
			//中断ボタン押下メッセージ受付のためメッセージ処理
			//本当はコピーをスレッド化してUI操作の受付をしておくべき。
			this.DoEvents();

			//0除算対応して進捗率の取得
			var progresspar = e.TotalFileSize > 0 
				? ( (decimal)e.TotalBytesTransferred / (decimal)e.TotalFileSize ) * (decimal)100 
				: (decimal)0;
			Dispatcher.Invoke(new Action(() =>
				{
					//プログレスバー更新
					this.progressBar1.Value = (int)progresspar;
				}));

			//中断処理判定
			if( _Stop )
			{
				e.CopyProgressResult = PInvoke.Win32API.CopyProgressResult.PROGRESS_STOP;
			}

		}

		/// <summary>
		/// コピーの中断を行う
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void btnCopyCancel_Click( object sender, RoutedEventArgs e )
		{
			_Stop = true;
		}

		/// <summary>
		/// 現在メッセージ待ち行列の中にある全てのUIメッセージを処理します。
		/// </summary>
		private void DoEvents()
		{
			DispatcherFrame frame = new DispatcherFrame();
			Dispatcher.CurrentDispatcher.BeginInvoke( DispatcherPriority.Background,
				new DispatcherOperationCallback( ExitFrames ), frame );
			Dispatcher.PushFrame( frame );
		}
		/// <summary>
		/// 実行ループに入るための手段
		/// </summary>
		private object ExitFrames( object f )
		{
			( (DispatcherFrame)f ).Continue = false;
			return null;
		}
	}
}
