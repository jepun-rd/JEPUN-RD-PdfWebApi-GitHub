//using iText.Commons.Utils;
//using iText.Kernel.Pdf;
//using iText.Kernel.Pdf.Colorspace;
//using iText.Kernel.Pdf.Xobject;
//using iText.Pdfoptimizer;
//using iText.Pdfoptimizer.Handlers.Imagequality.Processors;
//using iText.Pdfoptimizer.Handlers.Util;
//using iText.Pdfoptimizer.Report.Message;

//namespace Jepun.Core.Pdf.Compressor
//{
//    public class JepunImgCompressor : IImageProcessor
//    {
//        private readonly double compressionLevel;
//        private Func<byte[],int, byte[]> ImgFunc ;
//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="compressionLevel">is a compression coefficient. The value should be in range [0.0, 1.0]</param>
//        /// <exception cref="ArgumentException"></exception>
//        public JepunImgCompressor(double compressionLevel, Func<byte[], int, byte[]> func)
//        {
//            if (compressionLevel > 1.0 || compressionLevel < 0.0)
//            {
//                throw new ArgumentException(MessageFormatUtil.Format("Invalid compression parameter! Value {0} is out of range [0, 1]", compressionLevel));
//            }
//            this.compressionLevel = compressionLevel;
//            this.ImgFunc = func;
//        }

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="objectToProcess"></param>
//        /// <param name="session"></param>
//        /// <returns></returns>
//        public virtual PdfImageXObject ProcessImage(PdfImageXObject objectToProcess, OptimizationSession session)
//        {
//            PdfColorSpace pdfColorSpace = PdfColorSpace.MakeColorSpace(objectToProcess.GetPdfObject().Get(PdfName.ColorSpace));
//            if (pdfColorSpace is PdfDeviceCs.Cmyk)
//            {
//                session.RegisterEvent(SeverityLevel.WARNING, "Color space {0} is not supported by image processor {1}. Unable to optimize image with reference {2}", PdfName.DeviceCMYK, GetType(), objectToProcess.GetPdfObject().GetIndirectReference());
//                return objectToProcess;
//            }

//            if (pdfColorSpace is PdfSpecialCs.Indexed)
//            {
//                session.RegisterEvent(SeverityLevel.WARNING, "Color space {0} is not supported by image processor {1}. Unable to optimize image with reference {2}", PdfName.Indexed, GetType(), objectToProcess.GetPdfObject().GetIndirectReference());
//                return objectToProcess;
//            }

//            //byte[] data = ImageProcessingUtil.CompressJpeg(objectToProcess.GetImageBytes(), compressionLevel);
//            //objectToProcess.GetType();
            
//            byte[] data = ImgFunc(objectToProcess.GetImageBytes(), (int)(compressionLevel * 100));
//            //byte[] data = SkImgHelper.CompressImageQuality(objectToProcess.GetImageBytes(), (int)(compressionLevel * 100), format);
//            PdfStream pdfStream = (PdfStream)objectToProcess.GetPdfObject().Clone();
//            pdfStream.SetData(data);
//            pdfStream.Put(PdfName.Filter, PdfName.DCTDecode);
//            PdfArray asArray = objectToProcess.GetPdfObject().GetAsArray(PdfName.Mask);
//            if (asArray != null)
//            {
//                PdfStream value = BuildMask(asArray, new BitmapImagePixels(objectToProcess));
//                pdfStream.Put(PdfName.Mask, value);
//            }

//            return new PdfImageXObject(pdfStream);
//        }

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="maskArray"></param>
//        /// <param name="picture"></param>
//        /// <returns></returns>
//        private static PdfStream BuildMask(PdfArray maskArray, BitmapImagePixels picture)
//        {
//            MaskColors maskColors = MaskColors.Create(maskArray);
//            BitmapImagePixels bitmapImagePixels = new BitmapImagePixels(picture.GetWidth(), picture.GetHeight(), 1, 1);
//            for (int i = 0; i < picture.GetWidth(); i++)
//            {
//                for (int j = 0; j < picture.GetHeight(); j++)
//                {
//                    if (maskColors.IsColorMasked(picture.GetPixelAsLongs(i, j)))
//                    {
//                        bitmapImagePixels.SetPixel(i, j, new long[1] { 1L });
//                    }
//                }
//            }

//            PdfStream pdfStream = new PdfStream();
//            pdfStream.SetData(bitmapImagePixels.GetData());
//            pdfStream.Put(PdfName.Width, new PdfNumber(bitmapImagePixels.GetWidth()));
//            pdfStream.Put(PdfName.Height, new PdfNumber(bitmapImagePixels.GetHeight()));
//            pdfStream.Put(PdfName.ImageMask, new PdfBoolean(value: true));
//            pdfStream.Put(PdfName.Type, PdfName.XObject);
//            pdfStream.Put(PdfName.Subtype, PdfName.Image);
//            return pdfStream;
//        }
//    }
//}
