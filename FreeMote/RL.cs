﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace FreeMote
{
    /// <summary>
    /// Resource Loader
    /// </summary>
    public static class RL
    {
        public static Bitmap ConvertToImage(byte[] data, byte[] palette, int width, int height,
            PsbPixelFormat colorFormat, PsbPixelFormat paletteColorFormat)
        {
            //Copy data & palette to avoid changing original Data (might be reused)
            if (palette != null && palette.Length > 0)
            {
                return ConvertToImageWithPalette(data.ToArray(), palette.ToArray(), width, height, colorFormat, paletteColorFormat);
            }

            return ConvertToImage(data.ToArray(), width, height, colorFormat);
        }

        /// <summary>
        /// Convert a special format image to common image for extract
        /// </summary>
        /// <param name="data"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="colorFormat"></param>
        /// <returns></returns>
        public static Bitmap ConvertToImage(byte[] data, int width, int height,
            PsbPixelFormat colorFormat = PsbPixelFormat.None)
        {
            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bmpData = bmp.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly, bmp.PixelFormat);

            switch (colorFormat)
            {
                case PsbPixelFormat.BeRGBA4444:
                    data = Argb428(data);
                    Argb2Rgba(ref data);
                    break;
                case PsbPixelFormat.LeRGBA4444:
                    data = Argb428(data);
                    break;
                case PsbPixelFormat.BeRGBA8:
                    Switch_0_2(ref data);
                    break;
                case PsbPixelFormat.DXT1:
                    data = DxtUtil.DecompressDxt1(data, width, height);
                    Switch_0_2(ref data); //DXT1(for win) need conversion
                    break;
                case PsbPixelFormat.DXT5:
                    data = DxtUtil.DecompressDxt5(data, width, height);
                    Switch_0_2(ref data); //DXT5(for win) need conversion
                    break;
                case PsbPixelFormat.A8L8:
                    data = ReadA8L8(data, width, height);
                    break;
                case PsbPixelFormat.A8L8_SW:
                    data = ReadA8L8(data, width, height);
                    data = PostProcessing.UnswizzleTexture(data, width, height);
                    break;
                case PsbPixelFormat.TileA8L8_SW:
                    data = ReadA8L8(data, width, height);
                    data = PostProcessing.UntileTexture(data, width, height);
                    break;
                case PsbPixelFormat.BeRGBA8_SW:
                    data = PostProcessing.UnswizzleTexture(data, bmp.Width, bmp.Height);
                    Switch_0_2(ref data);
                    break;
                case PsbPixelFormat.LeRGBA8_SW:
                    data = PostProcessing.UnswizzleTexture(data, bmp.Width, bmp.Height);
                    break;
                case PsbPixelFormat.TileBeRGBA8_Rvl:
                    Argb2Rgba(ref data);
                    data = PostProcessing.UntileTextureRvl(data, bmp.Width, bmp.Height);
                    break;
                case PsbPixelFormat.FlipLeRGBA8_SW:
                    data = PostProcessing.UnswizzleTexture(data, bmp.Width, bmp.Height);
                    data = PostProcessing.FlipTexturePs3(data, width, height);
                    break;
                case PsbPixelFormat.FlipBeRGBA8_SW:
                    data = PostProcessing.UnswizzleTexture(data, bmp.Width, bmp.Height);
                    data = PostProcessing.FlipTexturePs3(data, width, height);
                    Switch_0_2(ref data);
                    Argb2Rgba(ref data, true);
                    break;
                case PsbPixelFormat.LeRGBA4444_SW:
                    data = Argb428(data);
                    //Rgba2Argb(ref data);
                    data = PostProcessing.UnswizzleTexture(data, bmp.Width, bmp.Height);
                    break;
                case PsbPixelFormat.TileLeRGBA4444_SW:
                    data = Argb428(data);
                    //Rgba2Argb(ref data);
                    data = PostProcessing.UntileTexture(data, bmp.Width, bmp.Height);
                    break;
                case PsbPixelFormat.TileLeRGBA8_SW:
                    data = PostProcessing.UntileTexture(data, bmp.Width, bmp.Height);
                    break;
                case PsbPixelFormat.TileBeRGBA8_SW:
                    data = PostProcessing.UntileTexture(data, bmp.Width, bmp.Height);
                    Argb2Rgba(ref data);
                    break;
                case PsbPixelFormat.A8:
                    data = ReadA8(data, width, height);
                    break;
                case PsbPixelFormat.A8_SW:
                    data = ReadA8(data, width, height);
                    data = PostProcessing.UnswizzleTexture(data, width, height);
                    break;
                case PsbPixelFormat.TileA8_SW:
                    data = ReadA8(data, width, height);
                    data = PostProcessing.UntileTexture(data, width, height);
                    break;
                case PsbPixelFormat.L8:
                    data = ReadL8(data, width, height);
                    break;
                case PsbPixelFormat.L8_SW:
                    data = ReadL8(data, width, height);
                    data = PostProcessing.UnswizzleTexture(data, width, height);
                    break;
                case PsbPixelFormat.TileL8_SW:
                    data = ReadL8(data, width, height);
                    data = PostProcessing.UntileTexture(data, width, height);
                    break;
                case PsbPixelFormat.RGB5A3:
                    data = ReadRgb5A3(data);
                    data = PostProcessing.UntileTextureRvl(data, width, height);
                    break;
                case PsbPixelFormat.RGBA5650:
                    data = ReadRgba5650(data);
                    break;
                case PsbPixelFormat.RGBA5650_SW:
                    data = ReadRgba5650(data);
                    data = PostProcessing.UnswizzleTexture(data, width, height);
                    break;
                case PsbPixelFormat.TileRGBA5650_SW:
                    data = ReadRgba5650(data);
                    data = PostProcessing.UntileTexture(data, width, height);
                    break;
                case PsbPixelFormat.ASTC_8BPP:
                    data = AstcDecoder.DecodeASTC(data, width, height, 4, 4);
                    break;
                case PsbPixelFormat.BC7:
                    data = new Bc7Decoder(data, width, height).Unpack();
                    break;
                case PsbPixelFormat.BC7_SW:
                    //  BC7 图像是 4x4 像素的块压缩，所以 width 和 height 参数应该是图像的块数，而不是像素数。因此需要将实际像素宽高除以 4
                    data = PostProcessing.UntileTextureV2(data, width / 4, height / 4, 16);
                    data = new Bc7Decoder(data, width, height).Unpack();
                    break;
            }

            int stride = bmpData.Stride; // 扫描线的宽度
            int offset = stride - width; // 显示宽度与扫描线宽度的间隙
            IntPtr iptr = bmpData.Scan0; // 获取bmpData的内存起始位置
            int scanBytes = stride * height; // 用stride宽度，表示这是内存区域的大小

            if (scanBytes >= data.Length)
            {
                Marshal.Copy(data, 0, iptr, data.Length);
                bmp.UnlockBits(bmpData); // 解锁内存区域

                //switch (colorFormat) // BMP post process
                //{
                //    case PsbPixelFormat.LeRGBA8_SW: // for PS3
                //        //bmp.RotateFlip(RotateFlipType.Rotate90FlipX); //This is obviously wrong way to flip it, only right when width == height
                //        break;
                //}
                return bmp;
            }

            throw new BadImageFormatException("data may not corresponding");
        }

        /// <summary>
        /// Convert a common image to special format image for build
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="pixelFormat"></param>
        /// <returns></returns>
        private static byte[] PixelBytesFromImage(Bitmap bmp, PsbPixelFormat pixelFormat = PsbPixelFormat.None)
        {
            //var bitsPerPixel = Image.GetPixelFormatSize(bmp.PixelFormat); //TODO: Check input bpp and convert?
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadOnly, bmp.PixelFormat);
            var bitDepth = Image.GetPixelFormatSize(bmp.PixelFormat);

            int stride = bmpData.Stride; // 扫描线的宽度
            int offset = stride - bmp.Width; // 显示宽度与扫描线宽度的间隙
            IntPtr iPtr = bmpData.Scan0; // 获取bmpData的内存起始位置
            int scanBytes = stride * bmp.Height; // 用stride宽度，表示这是内存区域的大小

            var result = new byte[scanBytes];
            Marshal.Copy(iPtr, result, 0, scanBytes);
            bmp.UnlockBits(bmpData); // 解锁内存区域

            switch (pixelFormat)
            {
                case PsbPixelFormat.LeRGBA4444:
                    result = Argb428(result, false);
                    break;
                case PsbPixelFormat.BeRGBA4444:
                    Argb2Rgba(ref result, true);
                    result = Argb428(result, false);
                    break;
                case PsbPixelFormat.BeRGBA8:
                    Switch_0_2(ref result);
                    break;
                case PsbPixelFormat.A8L8:
                    result = Argb2A8L8(result);
                    break;
                case PsbPixelFormat.A8L8_SW:
                    result = PostProcessing.SwizzleTexture(result, bmp.Width, bmp.Height, bitDepth);
                    result = Argb2A8L8(result);
                    break;
                case PsbPixelFormat.TileA8L8_SW:
                    result = PostProcessing.TileTexture(result, bmp.Width, bmp.Height, bitDepth);
                    result = Argb2A8L8(result);
                    break;
                case PsbPixelFormat.DXT1:
                    //Switch_0_2(ref result);
                    result = DxtUtil.Dxt1Encode(result, bmp.Width, bmp.Height);
                    break;
                case PsbPixelFormat.DXT5:
                    //Switch_0_2(ref result);
                    result = DxtUtil.Dxt5Encode(result, bmp.Width, bmp.Height);
                    break;
                case PsbPixelFormat.BeRGBA8_SW:
                    result = PostProcessing.SwizzleTexture(result, bmp.Width, bmp.Height, bitDepth);
                    Switch_0_2(ref result);
                    break;
                case PsbPixelFormat.LeRGBA8_SW:
                    result = PostProcessing.SwizzleTexture(result, bmp.Width, bmp.Height, bitDepth);
                    break;
                case PsbPixelFormat.FlipLeRGBA8_SW:
                    result = PostProcessing.FlipTexturePs3(result, bmp.Width, bmp.Height, bitDepth);
                    result = PostProcessing.SwizzleTexture(result, bmp.Width, bmp.Height, bitDepth);
                    break;
                case PsbPixelFormat.FlipBeRGBA8_SW:
                    result = PostProcessing.FlipTexturePs3(result, bmp.Width, bmp.Height, bitDepth);
                    result = PostProcessing.SwizzleTexture(result, bmp.Width, bmp.Height, bitDepth);
                    Argb2Rgba(ref result);
                    Switch_0_2(ref result);
                    break;
                case PsbPixelFormat.TileLeRGBA8_SW:
                    result = PostProcessing.TileTexture(result, bmp.Width, bmp.Height, bitDepth);
                    break;
                case PsbPixelFormat.TileBeRGBA8_SW:
                    result = PostProcessing.TileTexture(result, bmp.Width, bmp.Height, bitDepth);
                    Argb2Rgba(ref result, true);
                    break;
                case PsbPixelFormat.L8:
                    result = Argb2L8(result);
                    break;
                case PsbPixelFormat.A8_SW:
                    result = PostProcessing.SwizzleTexture(result, bmp.Width, bmp.Height, bitDepth);
                    result = Argb2A8(result);
                    break;
                case PsbPixelFormat.TileA8_SW:
                    result = PostProcessing.TileTexture(result, bmp.Width, bmp.Height, bitDepth);
                    result = Argb2A8(result);
                    break;
                case PsbPixelFormat.A8:
                    result = Argb2A8(result);
                    break;
                case PsbPixelFormat.CI4_SW_PSP:
                case PsbPixelFormat.CI8_SW_PSP:
                    result = PostProcessing.SwizzleTexture(result, bmp.Width, bmp.Height, bitDepth, SwizzleType.PSP);
                    break;
                case PsbPixelFormat.CI4_SW:
                case PsbPixelFormat.CI8_SW:
                    result = PostProcessing.SwizzleTexture(result, bmp.Width, bmp.Height, bitDepth);
                    break;
                case PsbPixelFormat.L8_SW:
                    result = PostProcessing.SwizzleTexture(result, bmp.Width, bmp.Height, bitDepth);
                    result = Argb2L8(result);
                    break;
                case PsbPixelFormat.TileL8_SW:
                    result = PostProcessing.TileTexture(result, bmp.Width, bmp.Height, bitDepth);
                    result = Argb2L8(result);
                    break;
                case PsbPixelFormat.LeRGBA4444_SW:
                    result = PostProcessing.SwizzleTexture(result, bmp.Width, bmp.Height, bitDepth);
                    result = Argb428(result, false);
                    break;
                case PsbPixelFormat.TileLeRGBA4444_SW:
                    result = PostProcessing.TileTexture(result, bmp.Width, bmp.Height, bitDepth);
                    result = Argb428(result, false);
                    break;
                case PsbPixelFormat.RGB5A3:
                    result = PostProcessing.TileTextureRvl(result, bmp.Width, bmp.Height, bitDepth);
                    result = Argb2Rgb5A3(result);
                    break;
                case PsbPixelFormat.TileCI4:
                case PsbPixelFormat.TileCI8:
                    result = PostProcessing.TileTextureRvl(result, bmp.Width, bmp.Height, bitDepth);
                    break;
                case PsbPixelFormat.RGBA5650:
                    result = Argb2Rgba5650(result);
                    break;
                case PsbPixelFormat.RGBA5650_SW:
                    result = PostProcessing.SwizzleTexture(result, bmp.Width, bmp.Height, bitDepth);
                    result = Argb2Rgba5650(result);
                    break;
                case PsbPixelFormat.TileRGBA5650_SW:
                    result = PostProcessing.TileTexture(result, bmp.Width, bmp.Height, bitDepth);
                    result = Argb2Rgba5650(result);
                    break;
            }

            return result;
        }

        /// <summary>
        /// Convert a PSB image resource which contains pal and palType to Bitmap. Only support BeRGBA8 & RGB5A3 palettes
        /// </summary>
        /// <param name="data"></param>
        /// <param name="palette"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="colorFormat"></param>
        /// <param name="paletteColorFormat"></param>
        /// <returns></returns>
        /// <exception cref="BadImageFormatException"></exception>
        public static Bitmap ConvertToImageWithPalette(byte[] data, byte[] palette, int width, int height,
            PsbPixelFormat colorFormat = PsbPixelFormat.None, PsbPixelFormat paletteColorFormat = PsbPixelFormat.None)
        {
            Bitmap bmp;
            BitmapData bmpData;

            switch (paletteColorFormat)
            {
                case PsbPixelFormat.BeRGBA8:
                    Switch_0_2(ref palette);
                    break;
                case PsbPixelFormat.RGB5A3:
                    palette = ReadRgb5A3(palette);
                    break;
            }

            if (colorFormat.IsCI_Tile())
            {
                data = PostProcessing.UntileTextureRvl(data, width, height, colorFormat.GetBitDepth()!.Value);
            }

            if (colorFormat.IsCI_SW())
            {
                data = PostProcessing.UnswizzleTexture(data, width, height, colorFormat.GetBitDepth()!.Value,
                    colorFormat.IsPSP_SW() ? SwizzleType.PSP : SwizzleType.PSV);
                //Switch_0_2(ref data);
            }

            switch (colorFormat)
            {
                case PsbPixelFormat.TileCI8:
                case PsbPixelFormat.CI8_SW_PSP:
                case PsbPixelFormat.CI8_SW:
                case PsbPixelFormat.CI8:
                    {
                        bmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
                        bmpData = bmp.LockBits(new Rectangle(0, 0, width, height),
                            ImageLockMode.WriteOnly, bmp.PixelFormat);
                        ColorPalette pal = bmp.Palette;
                        for (int i = 0; i < 256; i++)
                            pal.Entries[i] = Color.FromArgb(BitConverter.ToInt32(palette, i * 4));
                        // Assign the edited palette to the bitmap.
                        bmp.Palette = pal;
                    }

                    break;
                case PsbPixelFormat.TileCI4:
                case PsbPixelFormat.CI4_SW_PSP:
                case PsbPixelFormat.CI4_SW:
                case PsbPixelFormat.CI4:
                    {
                        bmp = new Bitmap(width, height, PixelFormat.Format4bppIndexed); //ここ重要
                        bmpData = bmp.LockBits(new Rectangle(0, 0, width, height),
                            ImageLockMode.WriteOnly, bmp.PixelFormat);
                        ColorPalette pal = bmp.Palette;
                        for (int i = 0; i < 16; i++)
                            pal.Entries[i] = Color.FromArgb(BitConverter.ToInt32(palette, i * 4));
                        // Assign the edited palette to the bitmap.
                        bmp.Palette = pal;
                        //data.Length * 2 = 8bppBmp.Width * 8bppBmp.Height
                    }
                    break;
                default:
                    return ConvertToImage(data, width, height, colorFormat);
            }

            int stride = bmpData.Stride; // 扫描线的宽度
            int offset = stride - width; // 显示宽度与扫描线宽度的间隙
            IntPtr iptr = bmpData.Scan0; // 获取bmpData的内存起始位置
            int scanBytes = stride * height; // 用stride宽度，表示这是内存区域的大小

            if (scanBytes >= data.Length)
            {
                //Marshal.Copy(data, 0, iptr, data.Length);
                Marshal.Copy(data, 0, iptr, data.Length);
                bmp.UnlockBits(bmpData); // 解锁内存区域
                
                return bmp;
            }

            throw new BadImageFormatException("data may not corresponding");
        }

        public static byte[] Compress(Stream stream, int align = 4)
        {
            return RleCompress.Compress(stream, align);
        }

        public static byte[] Compress(byte[] data, int align = 4)
        {
            using (var stream = new MemoryStream(data))
            {
                return Compress(stream, align);
            }
        }

        public static byte[] CompressImage(Bitmap image, PsbPixelFormat pixelFormat = PsbPixelFormat.None)
        {
            return Compress(PixelBytesFromImage(image, pixelFormat));
        }

        public static byte[] CompressImageFile(string path, PsbPixelFormat pixelFormat = PsbPixelFormat.None)
        {
            return CompressImage(new Bitmap(path, false), pixelFormat);
        }

        public static byte[] GetPixelBytesFromImageFile(string path, PsbPixelFormat pixelFormat = PsbPixelFormat.None)
        {
            Bitmap bmp = new Bitmap(path, false);
            return PixelBytesFromImage(bmp, pixelFormat);
        }

        public static byte[] GetPixelBytesFromImage(Image image, PsbPixelFormat pixelFormat = PsbPixelFormat.None)
        {
            if (!(image is Bitmap bmp))
            {
                bmp = new Bitmap(image);
            }

            return PixelBytesFromImage(bmp, pixelFormat);
        }

        public static void DecompressToImageFile(byte[] data, string path, int width, int height,
            PsbImageFormat format = PsbImageFormat.png, PsbPixelFormat colorFormat = PsbPixelFormat.None, int align = 4)
        {
            byte[] bytes;
            try
            {
                bytes = Decompress(data, width, height, align);
            }
            catch (Exception e)
            {
                throw new PsbBadFormatException(PsbBadFormatReason.Resources, "data incorrect", e);
            }

            ConvertToImageFile(bytes, path, width, height, format, colorFormat);
        }

        public static Bitmap DecompressToImage(byte[] data, int width, int height,
            PsbPixelFormat colorFormat = PsbPixelFormat.None, int align = 4)
        {
            byte[] bytes;
            try
            {
                bytes = Decompress(data, width, height, align);
            }
            catch (Exception e)
            {
                throw new PsbBadFormatException(PsbBadFormatReason.Resources, "data incorrect", e);
            }

            return ConvertToImage(bytes, width, height, colorFormat);
        }
        
        public static void ConvertToImageFile(byte[] data, string path, int width, int height, PsbImageFormat format,
            PsbPixelFormat colorFormat = PsbPixelFormat.None, byte[] palette = null,
            PsbPixelFormat paletteColorFormat = PsbPixelFormat.None)
        {
            Bitmap bmp = ConvertToImage(data, palette, width, height, colorFormat, paletteColorFormat);
            
            switch (format)
            {
                case PsbImageFormat.bmp:
                    bmp.Save(path, ImageFormat.Bmp);
                    break;
                case PsbImageFormat.png:
                    bmp.Save(path, ImageFormat.Png);
                    break;
            }
        }

        public static void ConvertToImageFileWithPalette(byte[] data, byte[] palette,string path, int width, int height, PsbPixelFormat colorFormat, PsbPixelFormat paletteColorFormat)
        {
            Bitmap bmp = ConvertToImageWithPalette(data, palette, width, height, colorFormat, paletteColorFormat);
            
            bmp.Save(path, ImageFormat.Png);
        }

        private static byte[] Decompress(Stream stream, int width, int height, int align = 4)
        {
            var realLength = height * width * align;
            return RleCompress.Decompress(stream, align, realLength);
        }

        public static byte[] Decompress(byte[] data, int width, int height, int align = 4)
        {
            using (var stream = new MemoryStream(data))
            {
                return Decompress(stream, width, height, align);
            }
        }

        public static byte[] Decompress(byte[] data, int align = 4)
        {
            using (var stream = new MemoryStream(data))
            {
                return RleCompress.Decompress(stream, align);
            }
        }

        #region Convert

        /// <summary>
        /// BGRA(LE ARGB) -> RGBA(BE RGBA)  (switch B &amp; R)
        /// </summary>
        /// <param name="bytes"></param>
        public static unsafe void Switch_0_2(ref byte[] bytes)
        {
            //RGBA in little endian is actually ABGR
            //Actually abgr -> argb
            fixed (byte* ptr = bytes)
            {
                int i = 0;
                int len = bytes.Length / 4;
                while (i < len)
                {
                    uint* iPtr = (uint*)ptr + i;
                    if (*iPtr != 0)
                    {
                        *iPtr = ((*iPtr & 0xFF000000)) |
                                ((*iPtr & 0x000000FF) << 16) |
                                ((*iPtr & 0x0000FF00)) |
                                ((*iPtr & 0x00FF0000) >> 16);
                    }

                    i++;
                }
            }
        }

        /// <summary>
        /// Set Alpha to 0xFF
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="visible"></param>
        public static unsafe void Rgbx2Rgba(ref byte[] bytes, bool visible = true)
        {
            fixed (byte* ptr = bytes)
            {
                int i = 0;
                int len = bytes.Length / 4;
                while (i < len)
                {
                    uint* iPtr = (uint*) ptr + i;
                    if (*iPtr != 0)
                    {
                        if (visible)
                        {
                            *iPtr = (*iPtr & 0xFFFFFF00) | 0x000000FF;
                        }
                        else
                        {
                            *iPtr = (*iPtr & 0xFFFFFF00);
                        } }

                    i++;
                }
            }
        }

        /// <summary>
        /// RGBA(BE) -> ARGB(LE BGRA) (switch A)
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="reverse">false: ROR; true: ROL</param>
        public static unsafe void Argb2Rgba(ref byte[] bytes, bool reverse = false)
        {
            //Actually bgra -> abgr
            fixed (byte* ptr = bytes)
            {
                int i = 0;
                int len = bytes.Length / 4;
                while (i < len)
                {
                    uint* iPtr = (uint*)ptr + i;
                    if (*iPtr != 0)
                    {
                        if (reverse)
                        {
                            *iPtr = ((*iPtr & 0xFF000000) >> 24) |
                                    ((*iPtr & 0x000000FF) << 8) |
                                    ((*iPtr & 0x0000FF00) << 8) |
                                    ((*iPtr & 0x00FF0000) << 8);
                        }
                        else
                        {
                            *iPtr = ((*iPtr & 0xFF000000) >> 8) |
                                    ((*iPtr & 0x000000FF) << 24) |
                                    ((*iPtr & 0x0000FF00) >> 8) |
                                    ((*iPtr & 0x00FF0000) >> 8);
                        }
                    }

                    i++;
                }
            }
        }

        public static byte[] ReadRgba5650(byte[] data)
        {
            var result = new byte[data.Length * 2];
            var shorts = MemoryMarshal.Cast<byte, ushort>(data.AsSpan());
            for (int i = 0; i < shorts.Length; i++)
            {
                var c2 = shorts[i];
                var r = (byte)((c2 >> 11) * 256u / 32u);
                var g = (byte)((c2 >> 5 & 0b00000_111111) * 256u / 64u);
                var b = (byte)((c2 & 0b00000_000000_11111) * 256u / 32u);
                //var a = data[i + 3];
                result[i * 4] = b;
                result[i * 4 + 1] = g;
                result[i * 4 + 2] = r;
                result[i * 4 + 3] = 0xFF;
            }

            return result;
        }

        public static byte[] ReadRgb5A3(byte[] data)
        {
            // https://wiki.tockdom.com/wiki/Image_Formats#RGB5A3
            var result = new byte[data.Length * 2];
            var shorts = MemoryMarshal.Cast<byte, ushort>(data.AsSpan()); //note the endianess issue here. Rvl looks like BE
            for (int i = 0; i < shorts.Length; i++)
            {
                var c2 = shorts[i];
                c2 = (ushort) ((c2 >> 8) | (c2 << 8)); //switch endianess, 0AAARRRRGGGGBBBB <- GGGGBBBB0AAARRRR
                byte r, g, b, a;
                if ((c2 & 0b1000000000000000) == 0) //RGB4A3
                {
                    a = (byte) ((c2 >> 12 & 0b111) * 0x20);
                    b = (byte) ((c2 >> 8 & 0b1111) * 0x11);
                    g = (byte) ((c2 >> 4 & 0b1111) * 0x11);
                    r = (byte) ((c2 & 0b1111) * 0x11);
                }
                else //RGB555
                {
                    a = 0xFF;
                    b = (byte) ((c2 >> 10 & 0b11111) * 0x8);
                    g = (byte) ((c2 >> 5 & 0b11111) * 0x8);
                    r = (byte) ((c2 & 0b11111) * 0x8);
                }

                result[i * 4] = r;
                result[i * 4 + 1] = g;
                result[i * 4 + 2] = b;
                result[i * 4 + 3] = a;
            }

            return result;
        }

        public static byte[] Argb2Rgb5A3(byte[] data)
        {
            var result = new byte[data.Length / 2];
            var shorts = MemoryMarshal.Cast<byte, ushort>(result.AsSpan());
            for (int i = 0; i < data.Length; i += 4)
            {
                byte r = data[i + 2];
                byte g = data[i + 1];
                byte b = data[i];
                byte a = data[i + 3];
                ushort c2;

                if (a < 0xE0) // RGB4A3
                {                    
                    c2 = (ushort)(((a >> 5) << 12) | ((r >> 4) << 8) | ((g >> 4) << 4) | (b >> 4));
                }
                else // RGB555
                {                    
                    c2 = (ushort)(0x8000 | ((r >> 3) << 10) | ((g >> 3) << 5) | (b >> 3));
                }
               
                c2 = (ushort)((c2 << 8) | (c2 >> 8)); //switch endianess
                shorts[i / 4] = c2;
            }

            return result;
        }

        public static byte[] Argb2Rgba5650(byte[] data)
        {
            var result = new byte[data.Length / 2];
            var shorts = MemoryMarshal.Cast<byte, ushort>(result.AsSpan());
            for (int i = 0; i < data.Length; i += 4)
            {
                var b = data[i];
                var g = data[i + 1];
                var r = data[i + 2];
                //var a = data[i + 3];
                ushort c2 = (ushort) (r * 32u / 256u << 11 | g * 64u / 256u << 5 | b * 32u / 256u);
                shorts[i / 4] = c2;
            }

            return result;
        }

        public static byte[] ReadRgba5551(byte[] data)
        {
            var result = new byte[data.Length * 2]; 
            var shorts = MemoryMarshal.Cast<byte, ushort>(data.AsSpan());
            for (int i = 0; i < shorts.Length; i++)
            {
                var c2 = shorts[i];
                var r = (byte)((c2 >> 11) * 256u / 32u);
                var g = (byte)((c2 >> 6 & 0b11111) * 256u / 32u);
                var b = (byte)((c2 >> 1 & 0b11111) * 256u / 32u);
                var a = (byte)(c2 & 1);
                result[i * 4] = b;
                result[i * 4 + 1] = g;
                result[i * 4 + 2] = r;
                result[i * 4 + 3] = a;
            }

            return result;
        }

        public static byte[] Argb2Rgba5551(byte[] data)
        {
            var result = new byte[data.Length / 2];
            var shorts = MemoryMarshal.Cast<byte, ushort>(result.AsSpan());
            for (int i = 0; i < data.Length; i += 4)
            {
                var b = data[i];
                var g = data[i + 1];
                var r = data[i + 2];
                var a = data[i + 3] == (byte)0x0 ? (byte)0 : (byte)1;
                ushort c2 = (ushort)(r * 32u / 256u << 11 | g * 32u / 256u << 6 | b * 32u / 256u << 1 | a);
                shorts[i / 4] = c2;
            }

            return result;
        }

        /// <summary>
        /// RGBA4444 &amp; RGBA8 conversion
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="extend">true: 4 to 8; false: 8 to 4</param>
        /// Shibuya Scramble!
        public static byte[] Argb428(byte[] bytes, bool extend = true)
        {
            if (extend)
            {
                var result = new byte[bytes.Length * 2];
                var dst = 0;
                for (int i = 0; i < bytes.Length; i += 2)
                {
                    var p = BitConverter.ToUInt16(bytes, i);
                    result[dst++] = (byte)((p & 0x000Fu) * 0xFFu / 0x000Fu);
                    result[dst++] = (byte)((p & 0x00F0u) * 0xFFu / 0x00F0u);
                    result[dst++] = (byte)((p & 0x0F00u) * 0xFFu / 0x0F00u);
                    result[dst++] = (byte)((p & 0xF000u) * 0xFFu / 0xF000u);
                }

                return result;
            }
            else
            {
                var result = new byte[bytes.Length / 2];
                for (int i = 0; i < result.Length; i += 2)
                {
                    ushort p = (ushort)((bytes[i * 2] / 16) |
                                         (bytes[i * 2 + 1] / 16) << 4 |
                                         (bytes[i * 2 + 2] / 16) << 8 |
                                         (bytes[i * 2 + 3] / 16) << 12);
                    BitConverter.GetBytes(p).CopyTo(result, i);
                }

                return result;
            }
        }
        
        public static byte[] Argb2L8(byte[] data)
        {
            byte[] output = new byte[data.Length / 4];
            int dst = 0;

            for (int i = 0; i < data.Length; i += 4)
            {
                byte c = (byte)((data[i] + data[i + 1] + data[i + 2]) / 3);
                //byte a = 0xFF;
                output[dst++] = c;
                //output[dst++] = a;
            }

            return output;
        }

        private static byte[] Argb2A8(byte[] data)
        {
            byte[] output = new byte[data.Length / 4];
            int dst = 0;

            for (int i = 0; i < data.Length; i += 4)
            {
                //byte c = (byte)((data[i] + data[i + 1] + data[i + 2]) / 3);
                byte a = data[i + 3];
                output[dst++] = a;
                //output[dst++] = a;
            }

            return output;
        }

        #endregion

        #region Read

        private static byte[] ReadA8L8(byte[] data, int width, int height)
        {
            byte[] output = new byte[height * width * 4];
            int dst = 0;

            for (int i = 0; i < width * height; i++)
            {
                if (2 * i + 1 > data.Length)
                {
                    break;
                }

                byte c = data[2 * i];
                byte a = data[2 * i + 1];
                output[dst++] = c;
                output[dst++] = c;
                output[dst++] = c;
                output[dst++] = a;
            }

            return output;
        }

        private static byte[] Argb2A8L8(byte[] data)
        {
            byte[] output = new byte[data.Length / 2];
            int dst = 0;

            for (int i = 0; i < data.Length; i += 4)
            {
                byte c = (byte)((data[i] + data[i + 1] + data[i + 2]) / 3);
                byte a = data[i + 3];
                output[dst++] = c;
                output[dst++] = a;
            }

            return output;
        }

        private static byte[] ReadA8(byte[] data, int width, int height)
        {
            byte[] output = new byte[height * width * 4];
            int dst = 0;

            for (int i = 0; i < width * height; i++)
            {
                if (i > data.Length)
                {
                    break;
                }

                byte c = 0xFF;
                byte a = data[i];
                output[dst++] = c;
                output[dst++] = c;
                output[dst++] = c;
                output[dst++] = a;
            }

            return output;
        }

        public static byte[] ReadL8(byte[] data, int width, int height)
        {
            byte[] output = new byte[height * width * 4];
            int dst = 0;

            for (int i = 0; i < width * height; i++)
            {
                if (i > data.Length)
                {
                    break;
                }

                byte c = data[i];
                byte a = 0xFF;
                output[dst++] = c;
                output[dst++] = c;
                output[dst++] = c;
                output[dst++] = a;
            }

            return output;
        }

        #endregion

    }
}