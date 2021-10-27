using System;
using System.IO;

namespace Photon.Voice
{
	public class RawCodec
	{
		public class Encoder<T> : IEncoderDirect<T[]>
		{
			public string Error { get; private set; }

			public Action<ArraySegment<byte>, FrameFlags> Output { set; get; }

            int sizeofT = System.Runtime.InteropServices.Marshal.SizeOf(default(T));
			byte[] byteBuf = new byte[0];
			private static readonly ArraySegment<byte> EmptyBuffer = new ArraySegment<byte>(new byte[] { });

			public ArraySegment<byte> DequeueOutput(out FrameFlags flags)
			{
                flags = 0;
                return EmptyBuffer;
			}

			public void EndOfStream()
			{
			}

			public I GetPlatformAPI<I>() where I : class
			{
				return null;
			}

			public void Dispose()
			{				
			}			

			public void Input(T[] buf)
			{
				if (Error != null)
				{
					return;
				}
				if (Output == null)
				{
					Error = "RawCodec.Encoder: Output action is not set";
					return;
				}
				if (buf == null)
				{
					return;
				}
				if (buf.Length == 0)
				{
					return;
				}

				var s = buf.Length * sizeofT;
				if (byteBuf.Length < s)
				{
					byteBuf = new byte[s];
				}
				Buffer.BlockCopy(buf, 0, byteBuf, 0, s);
				Output(new ArraySegment<byte>(byteBuf, 0, s), 0);
			}
		}

		public class Decoder<T> : IDecoder
		{
			public string Error { get; private set; }

			public Decoder(Action<FrameOut<T>> output)
			{
				this.output = output;
			}

			public void Open(VoiceInfo info)
			{
			}
			
			private Type outType = typeof(T);
			T[] buf = new T[0];
			int sizeofT = System.Runtime.InteropServices.Marshal.SizeOf(default(T));

			public void Input(byte[] byteBuf, FrameFlags flags)
			{
				if (byteBuf == null)
				{
					return;
				}
				if (byteBuf.Length == 0)
				{
					return;
				}

				var s = byteBuf.Length / sizeofT;
				if (buf.Length < s)
				{
					buf = new T[s];
				}
				Buffer.BlockCopy(byteBuf, 0, buf, 0, byteBuf.Length);

				output(new FrameOut<T>((T[])(object)buf, false));
			}
			public void Dispose()
			{
			}

			private Action<FrameOut<T>> output;
		}
	}
}
