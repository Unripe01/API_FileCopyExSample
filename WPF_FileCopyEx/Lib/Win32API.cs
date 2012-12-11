using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace WPF_FileCopyEx.PInvoke
{
	/// <summary>
	/// Win32API CopyFileEXの実装
	/// </summary>
	class Win32API
	{
		#region "WinAPI"
		/// <summary>
		///  進捗状況を通知するコピー操作を提供します。
		/// </summary>
		/// <param name="lpExistingFileName">コピー元ファイルの名前</param>
		/// <param name="lpNewFileName">新規ファイルの名前</param>
		/// <param name="lpProgressRoutine">コールバック関数</param>
		/// <param name="lpData">コールバック関数に渡すパラメータ</param>
		/// <param name="pbCancel">操作の取り消しに使います</param>
		/// <param name="dwCopyFlags">ファイルのコピー方法を指定する</param>
		/// <returns></returns>
		[DllImport( "kernel32.dll", EntryPoint="CopyFileEx", SetLastError=true, CharSet=CharSet.Auto )]
		[return: MarshalAs( UnmanagedType.Bool )]
		private static extern bool _CopyFileEx(
			  string lpExistingFileName
			, string lpNewFileName
			, CopyProgressRoutine lpProgressRoutine
			, IntPtr lpData
			, ref Int32 pbCancel
			, CopyFileFlags dwCopyFlags
		);

		/// <summary>
		/// CopyFileEx実行時に指定するコールバック関数
		/// </summary>
		/// <param name="TotalFileSize">バイト単位の総ファイルサイズ</param>
		/// <param name="TotalBytesTransferred">転送された総バイト数</param>
		/// <param name="StreamSize">このストリームの総バイト数</param>
		/// <param name="StreamBytesTransferred">このストリームに対して転送された総バイト数</param>
		/// <param name="dwStreamNumber">現在のストリーム</param>
		/// <param name="dwCallbackReason">CopyProgressRoutine 関数が呼び出された理由</param>
		/// <param name="hSourceFile">コピー元ファイルのハンドル</param>
		/// <param name="hDestinationFile">コピー先ファイルのハンドル</param>
		/// <param name="lpData">CopyFileEx 関数から渡されるパラメータ</param>
		/// <returns></returns>
		private delegate CopyProgressResult CopyProgressRoutine(
			  long TotalFileSize
			, long TotalBytesTransferred
			, long StreamSize						
			, long StreamBytesTransferred
			, uint dwStreamNumber
			, CopyProgressCallbackReason dwCallbackReason	
			, IntPtr hSourceFile
			, IntPtr hDestinationFile
			, IntPtr lpData
		);

		/// <summary>
		/// コピー操作の進捗状況通知時に送る後続処理命令
		/// Windows Vista以降は、トランザクションNTFS（TxF）が採用されており
		/// PROGRESS_STOPの挙動がXP版とは異なるので注意。（コピー先ファイルが必ず消される）
		/// トランザクションNTFSはWindows7以降では廃止の方向なので、これもまた注意。
		/// </summary>
		public enum CopyProgressResult : uint
		{
			/// <summary>コピー操作を続行します。</summary>
			PROGRESS_CONTINUE = 0,
			/// <summary>コピー操作を取り消し、コピー先ファイルを削除します。</summary>
			PROGRESS_CANCEL = 1,
			/// <summary>コピー操作を停止します。コピー操作は後で再実行することができます。</summary>
			PROGRESS_STOP = 2,
			/// <summary>コピー操作を続行しますが、進捗状況をレポートする CopyProgressRoutine 関数を起動しません。</summary>
			PROGRESS_QUIET = 3
		}

		/// <summary>
		/// CopyProgressRoutine 関数が呼び出された理由を表します
		/// </summary>
		public enum CopyProgressCallbackReason : uint
		{
			/// <summary>データファイルの別の部分がコピーされました。</summary>
			CALLBACK_CHUNK_FINISHED = 0x00000000,
			/// <summary>別のストリームがすでに作成され、コピーされようとしています。コールバックルーチンが最初に呼び出されたときにこの理由が返されます。</summary>
			CALLBACK_STREAM_SWITCH = 0x00000001
		}

		/// <summary>
		/// ファイルをコピーする方法を指定するフラグ
		/// </summary>
		[Flags]
		private enum CopyFileFlags : uint
		{
			/// <summary>コピー先の内容を暗号化できない場合でも、暗号化されたファイルをコピーしようが成功します。</summary>
			COPY_FILE_ALLOW_DECRYPTED_DESTINATION = 0x00000008,
			/// <summary>ソース ファイルがシンボリック リンクの場合は、リンク先のファイルもソースのシンボリック リンクが指している同じファイルを指すシンボリック リンクです。Windows Server 2003 および Windows XP:この値はサポートされていません。</summary>
			COPY_FILE_COPY_SYMLINK = 0x00000800,
			/// <summary>ターゲット ファイルが既に存在する場合、コピー操作は直ちに失敗します。</summary>
			COPY_FILE_FAIL_IF_EXISTS = 0x00000001,
			/// <summary>コピー操作は、システム I/O のキャッシュ ・ リソースをバイパスして、バッファーなしの I/O を使用して実行されます。非常に大きなファイルの転送をお勧めします。Windows Server 2003 および Windows XP:この値はサポートされていません。</summary>
			COPY_FILE_NO_BUFFERING = 0x000010000,
			/// <summary>ファイルがコピーされ、元のファイルが書き込みアクセスで開かれます。</summary>
			COPY_FILE_OPEN_SOURCE_FOR_WRITE = 0x00000004,
			/// <summary>コピーに失敗した場合、ターゲット ファイルにコピーの進行状況が追跡されます。失敗したコピーは、あとで失敗した呼び出しで使用されるものとlpExistingFileNameとlpNewFileNameのための同じ値を指定して再起動できます。新しいファイルは、コピー操作中に複数回フラッシュ可能性があり、ダウンコピー操作が著しく低下することができます。</summary>
			COPY_FILE_RESTARTABLE = 0x00000002
		}
		#endregion "WinAPI"

		#region "C#"

		/// <summary>
		/// 進捗状況通知イベントのパラメータ
		/// </summary>
		public class CopyProgressEventArgs : EventArgs
		{
			///<summary>バイト単位の総ファイルサイズ</summary>
			public long TotalFileSize { get; internal set; }
			///<summary>転送された総バイト数</summary>
			public long TotalBytesTransferred { get; internal set; }
			///<summary>このストリームの総バイト数</summary>
			public long StreamSize { get; internal set; }
			///<summary>このストリームに対して転送された総バイト数</summary>
			public long StreamBytesTransferred { get; internal set; }
			///<summary>現在のストリーム</summary>
			public uint StreamNumber { get; internal set; }
			///<summary>イベントが発生した理由</summary>
			public CopyProgressCallbackReason CallbackReason { get; internal set; }
			///<summary>処理の継続・中止指示</summary>
			public CopyProgressResult CopyProgressResult { get; set; }
		}
		private CopyProgressEventArgs _CopyProgressEventArgs = new CopyProgressEventArgs();

		/// <summary>
		/// 進捗状況通知イベントハンドラ
		/// </summary>
		/// <param name="s"></param>
		/// <param name="e"></param>
		public delegate void CopyProgressEventHandler( object s, CopyProgressEventArgs e );

		/// <summary>
		/// 進捗状況通知イベント
		/// </summary>
		public event CopyProgressEventHandler ProgressChanged;

		/// <summary>
		/// コピー操作の継続・中止フラグ設定に利用
		/// 当クラスでは未使用にしているが、
		/// 0 はFalse、それ以外はTrueとして扱う。0以外の値をセットしたらキャンセル処理が行われる
		/// </summary>
		private int _CopyCancel;

		/// <summary>
		/// コピー操作結果ステータス
		/// </summary>
		public enum ResultStatus
		{
			/// <summary>完了しました。</summary>
			Completed,
			/// <summary>処理に失敗しました</summary>
			Failed,
			/// <summary>ユーザーによる中断が行われました</summary>
			Stoped
		}

		/// <summary>win32の「要求は中断されました。」エラーコード</summary>
		private const int WIN32_ERROR_CODE_SUSPENDED = 1235;

		/// <summary>
		/// 進捗状況を通知するコピー処理を開始します。
		/// </summary>
		/// <param name="sourceFilePath">コピー元ファイルパス</param>
		/// <param name="destinationFilePath">コピー先ファイルパス</param>
		/// <param name="overWrite">上書き設定（true:上書き可、false:上書き不可）</param>
		/// <returns></returns>
		public ResultStatus CopyStart( string sourceFilePath, string destinationFilePath, bool overWrite )
		{
			//上書き処理の可否
			CopyFileFlags ov = overWrite 
				? CopyFileFlags.COPY_FILE_RESTARTABLE 
				: CopyFileFlags.COPY_FILE_FAIL_IF_EXISTS;

			//API CopyFileExを実行
			bool isSuccess = _CopyFileEx( 
				sourceFilePath
				, destinationFilePath
				, new CopyProgressRoutine( CopyProgressRoutineCallBack )
				, IntPtr.Zero
				, ref _CopyCancel
				, ov
			);

			if( isSuccess )
			{
				return ResultStatus.Completed;
			}

			//中断も失敗もエラーとして判断される
			int errCode = Marshal.GetLastWin32Error();
			if( errCode == WIN32_ERROR_CODE_SUSPENDED )
			{
				//ユーザーによる中断意外でも1235が返ってくる場合がある時は別途判断する必要がある。
				return ResultStatus.Stoped;
			}
			return ResultStatus.Failed;
		}

		/// <summary>
		/// _CopyFileExに引き渡すコールバック関数
		/// ラップしてCopyProgressEventHandlerを発生させます
		/// </summary>
		/// <returns></returns>
		private CopyProgressResult CopyProgressRoutineCallBack(
			long totalFileSize
			, long totalBytesTransferred
			, long streamSize
			, long streamBytesTransferred
			, uint streamNumber
			, CopyProgressCallbackReason callbackReason
			, IntPtr hSourceFile
			, IntPtr hDestinationFile
			, IntPtr lpData
			)
		{
			//イベント未登録ならそのまま継続
			if( ProgressChanged == null )
			{
				return CopyProgressResult.PROGRESS_CONTINUE;
			}
			_CopyProgressEventArgs.TotalFileSize = totalFileSize;
			_CopyProgressEventArgs.TotalBytesTransferred = totalBytesTransferred;
			_CopyProgressEventArgs.StreamSize = streamSize;
			_CopyProgressEventArgs.StreamBytesTransferred = streamBytesTransferred;
			_CopyProgressEventArgs.StreamNumber = streamNumber;
			_CopyProgressEventArgs.CallbackReason = callbackReason;
			_CopyProgressEventArgs.CopyProgressResult = CopyProgressResult.PROGRESS_CONTINUE;
			
			//利用者側のイベント発行
			ProgressChanged( this, _CopyProgressEventArgs );

			//変更された処理継続指示を返却し、_CopyFileExに後続の処理を委ねる
			return _CopyProgressEventArgs.CopyProgressResult;
		}
		#endregion "C#"
	}
}
