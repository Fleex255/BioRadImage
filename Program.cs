using System;
using System.Text;
using System.IO;
using System.Drawing;

namespace BioRadImage {
    class Program {
        static bool noWait = false;
        static void Main(string[] args) {
            if (args.Length == 0) {
                Console.WriteLine("Usage: BIORADIMAGE InFile [OutFile] [/FORCE] [/NOWAIT]");
                Console.WriteLine("       InFile:  Path to Bio-Rad raw image file");
                Console.WriteLine("       OutFile: Path to new BMP file (optional)");
                Console.WriteLine("       /force:  Don't check for signature before attempting conversion");
                Console.WriteLine("       /nowait: Don't wait for a key press before exiting");
                Exit(1);
            }
            bool force = false;
            for (int i = 1; i < args.Length; i++) {
                if (args[i].Equals("/force", StringComparison.InvariantCultureIgnoreCase)) {
                    force = true;
                } else if (args[i].Equals("/nowait", StringComparison.InvariantCultureIgnoreCase)) {
                    noWait = true;
                }
            }
            if (!File.Exists(args[0])) {
                Console.WriteLine("Can't find input file");
                Exit(1);
            }
            Bitmap bitmap;
            using (FileStream input = new FileStream(args[0], FileMode.Open)) {
                if (!force) {
                    input.Position = 0x38;
                    String sigText = "Bio-Rad Scan File";
                    byte[] sig = new byte[sigText.Length];
                    input.Read(sig, 0, sigText.Length);
                    if (!Encoding.ASCII.GetString(sig).Equals(sigText)) {
                        Console.WriteLine("Invalid signature - use /force to proceed anyway");
                        Exit(1);
                    }
                }
                input.Position = 0x172;
                int startPtr = ReadInt16BE(input);
                input.Position = startPtr - 0x4A6;
                int width = ReadInt16BE(input);
                int height = ReadInt16BE(input);
                Console.WriteLine("Image is " + width + "x" + height + " pixels");
                bitmap = new Bitmap(width, height);
                input.Position = startPtr;
                for (int y = height - 1; y >= 0; y--) {
                    for (int x = 0; x < width; x++) {
                        int lightness = input.ReadByte();
                        bitmap.SetPixel(x, y, Color.FromArgb(lightness, lightness, lightness));
                    }
                }
            }
            string newFile;
            if (args.Length > 1 && !args[1].StartsWith("/")) {
                newFile = args[1];
            } else {
                string fileTitle = Path.GetFileNameWithoutExtension(args[0]);
                newFile = Path.Combine(Path.GetDirectoryName(args[0]), fileTitle + ".bmp");
            }
            bitmap.Save(newFile);
            Console.WriteLine("Output saved as " + newFile);
            Exit(0);
        }
        static void Exit(int Code) {
            if (!noWait) {
                Console.WriteLine("Press any key to exit");
                Console.ReadKey(true);
            }
            Environment.Exit(Code);
        }
        static int ReadInt16BE(FileStream stream) {
            int high = stream.ReadByte();
            int low = stream.ReadByte();
            return high * 0x100 + low;
        }
    }
}
