
using iText.Bouncycastle.Crypto;
using iText.Bouncycastle.X509;
using iText.Commons.Bouncycastle.Cert;
using iText.Commons.Utils;
using iText.Forms;
using iText.Forms.Fields.Properties;
using iText.Forms.Form.Element;
using iText.Html2pdf;
using iText.IO.Font;
//using iText.Licensing.Base.Strategy;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Exceptions;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Action;
using iText.Kernel.Pdf.Annot;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Extgstate;
using iText.Kernel.Pdf.Filespec;
using iText.Layout.Element;
using iText.Signatures;
using iText.Signatures;
//using iText.Pdfoptimizer;
//using iText.Pdfoptimizer.Handlers;
//using Jepun.Core.Pdf.Compressor;
using Jepun.Core.Pdf.Model;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;
using System;
using System.Text;
namespace Jepun.Core.Pdf
{
	/// <summary>
	/// https://github.com/jepun-rd/Jepun.Core.Pdf
	/// </summary>
	public static class PdfHelper
    {
		#region IText7 Core
        /// <summary>
        /// 檢測是否受保護
        /// </summary>
        /// <param name="pdfFile"></param>
        /// <returns>true:保護 無法編輯  false:未保護</returns>
		public static bool IsProtected(byte[] pdfFile)
		{
            try
            {            
			    using (var input = new MemoryStream(pdfFile))			 
			    {
                    var pdfDoc = new PdfDocument((new PdfReader(input)));
				    return pdfDoc.GetReader().IsEncrypted();
			    }
			}
            catch(Exception ex) 
            {
                throw ex;           
            }
		}
        /// <summary>
        /// PDF解密
        /// </summary>
        /// <param name="pdfFile"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static byte[] DecryptPDF(byte[] pdfFile, string password)
        {
            if (pdfFile == null || pdfFile.Length == 0)
            {
                throw new ArgumentNullException(nameof(pdfFile), "輸入的 PDF 位元組陣列不能為空。");
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password), "解密密碼不能為空。");
            }

            using (var input = new MemoryStream(pdfFile))
            using (var output = new MemoryStream())
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                try
                {
                    // 1. 設定 PdfReaderProperties，將密碼傳遞進去
                    ReaderProperties readerProperties = new ReaderProperties().SetPassword(passwordBytes);
                    // 2. 創建 PdfReader，它會嘗試使用密碼開啟輸入流
                    // 3. 創建 PdfWriter，將內容寫入輸出流
                    // 注意：這裡不需要額外的 WriterProperties 來設定密碼，因為我們的目標是解密後輸出一個不加密的 PDF
                    // 4. 創建 PdfDocument，連接 Reader 和 Writer
                    // 這個步驟會將 PdfReader 讀取的內容 (解密後的) 透過 PdfWriter 寫入到 pdfOutputStream
                    var pdfDoc = new PdfDocument(new PdfReader(input, readerProperties), new PdfWriter(output));
                    // 5. 關閉 PdfDocument，確保所有內容寫入完成
                    pdfDoc.Close();
                    // 6. 返回解密後的 PDF 位元組陣列
                    return output.ToArray();
                }
                catch (Exception ex)
                {
                    // 捕獲其他一般異常
                    throw new InvalidOperationException($"解密 PDF 時發生錯誤: {ex.Message}", ex);
                }
            }
        }
        /// <summary>
        /// pdf加密
        /// </summary>
        /// <param name="reader">來源</param>   
        /// <param name="userPwd">user密碼</param>
        /// <param name="strength">強度(高:安全,但耗時)</param>
        /// <param name="owrPwd">owner密碼</param>
        /// <param name="pmss">權限(ex. EncryptionConstants.ALLOW_SCREENREADERS)</param>
        public static byte[] EncryptPDF(byte[] pdfFile, string userPwd, bool strength = false, string owrPwd = "jepun", int pmss = EncryptionConstants.ALLOW_SCREENREADERS)
        {

            using (var input = new MemoryStream(pdfFile))
            using (var output = new MemoryStream())
            {
                var writerProperties = new WriterProperties();
                if (strength)
                {
                    writerProperties.SetStandardEncryption(Encoding.UTF8.GetBytes(userPwd), Encoding.UTF8.GetBytes(owrPwd), pmss, EncryptionConstants.ENCRYPTION_AES_256);
                }
                else
                {
                    writerProperties.SetStandardEncryption(Encoding.UTF8.GetBytes(userPwd), Encoding.UTF8.GetBytes(owrPwd), pmss, EncryptionConstants.ENCRYPTION_AES_128);
                }

                var pdfDoc = new PdfDocument(new PdfReader(input), new PdfWriter(output, writerProperties));
                pdfDoc.Close();

                return output.ToArray();
            }
        }

        /// <summary>
        /// 加簽章檔案加到PDF,每頁都加到一個指定位置
        /// </summary>
        /// <param name="pdfFile">PDF檔案</param>    
        /// <param name="signFile">簽章檔案</param>
        /// <param name="newWidth">寬</param>
        /// <param name="newHeight">高</param>
        /// <param name="absoluteX">X位置</param>
        /// <param name="absoluteY">Y位置</param>
        /// <returns></returns>
        public static byte[] AddStamper(byte[] pdfFile, byte[] signFile, float newWidth, float newHeight, float absoluteX, float absoluteY, float rotationDegrees = 0)
        {

            using (var input = new MemoryStream(pdfFile))
            using (var output = new MemoryStream())
            {
                using (var pdfDoc = new PdfDocument(new PdfReader(input), new PdfWriter(output)))
                {
                    ImageData img = ImageDataFactory.Create(signFile);
                    for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
                    {
                        PdfPage page = pdfDoc.GetPage(i);
                        PdfCanvas canvas = new PdfCanvas(page);
                        if (page.GetRotation() > 0)
                        {
                            switch (page.GetRotation())
                            {
                                case 90:
                                    canvas.ConcatMatrix(0.0, 1.0, -1.0, 0.0, page.GetPageSizeWithRotation().GetTop(), 0.0);
                                    break;
                                case 180:
                                    canvas.ConcatMatrix(-1.0, 0.0, 0.0, -1.0, page.GetPageSizeWithRotation().GetRight(), page.GetPageSizeWithRotation().GetTop());
                                    break;
                                case 270:
                                    canvas.ConcatMatrix(0.0, -1.0, 1.0, 0.0, 0.0, page.GetPageSizeWithRotation().GetRight());
                                    break;
                            }
                        }
                        canvas.SaveState().AddImageWithTransformationMatrix(img, newWidth, 0, 0, newHeight, absoluteX, absoluteY, false);
                    }
                    //PdfDocumentInfo info = pdfDoc.GetDocumentInfo();
                    //info.SetTitle("測試中文標題");
                    //info.SetAuthor("Fixed Author");
                    //info.SetCreator("Fixed Creator");
                    //Dictionary<string, string> pdfinfodata = new Dictionary<string, string>();
                    ////D:20230526102520+08'00'
                    //pdfinfodata.Add("CreationDate", $"D:{new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc).ToString("yyyyMMddHHmmss")}+08'00'");
                    //pdfinfodata.Add("ModDate", $"D:{new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc).ToString("yyyyMMddHHmmss")}+08'00'");
                    //pdfinfodata.Add("Producer", "");
                    //info.SetMoreInfo(pdfinfodata);

                    //XMPMeta xmpMeta = XMPMetaFactory.Create();
                    //xmpMeta.SetProperty(XMPConst.NS_DC, "dc:title", "測試中文標題");
                    //xmpMeta.SetProperty(XMPConst.NS_DC, "dc:creator", "Fixed Author");
                    //xmpMeta.SetProperty(XMPConst.NS_XMP, "xmp:CreatorTool", "Fixed Creator");

                    //// 设置固定的创建时间和修改时间
                    //DateTime fixedDateTime = new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc);
                    //string fixedDateString = fixedDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");//new PdfDate(new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc)).GetW3CDate();
                    ////xmpMeta.SetProperty(XMPConst.NS_XMP, "xmp:CreationDate", fixedDateString);
                    ////xmpMeta.SetProperty(XMPConst.NS_XMP, "xmp:ModDate", fixedDateString);
                    //xmpMeta.SetProperty(XMPConst.NS_XMP, "xmp:CreateDate", fixedDateString);
                    //xmpMeta.SetProperty(XMPConst.NS_XMP, "xmp:ModifyDate", fixedDateString);
                    //xmpMeta.SetProperty(XMPConst.NS_XMP, "xmp:MetadataDate", fixedDateString);

                    //// 应用XMP元数据到PDF
                    //pdfDoc.SetXmpMetadata(xmpMeta);


                    //byte[] fixedId = System.Text.Encoding.ASCII.GetBytes("FixedFileIdentifier");
                    //PdfString id1 = new PdfString(fixedId);
                    //PdfString id2 = new PdfString(fixedId);
                    //PdfArray idArray = new PdfArray();
                    //idArray.Add(id1);
                    //idArray.Add(id2);
                    //pdfDoc.GetTrailer().Put(PdfName.ID, idArray);



                    ////pdfDoc.GetCatalog().SetVersion(PdfVersion.PDF_1_7);
                    //for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
                    //{
                    //    PdfPage page = pdfDoc.GetPage(i);
                    //    page.Flush();
                    //}

                }
                return output.ToArray();
            }
        }
		/// <summary>
		/// 壓印PDF
		/// </summary>
		/// <param name="pdfFile">PDF檔案</param>
		/// <param name="pdfStamperData">壓印定義</param>
		/// <returns></returns>
		public static byte[] AddStamper(byte[] pdfFile, PdfStamperData pdfStamperData)
        {
            byte[] retVal = Array.Empty<byte>();
            MemoryStream outStream = null;
            PdfDocument pdfDoc = null;
            try
            {
                outStream = new MemoryStream();
                pdfDoc = new PdfDocument(new PdfReader(new MemoryStream(pdfFile)), new PdfWriter(outStream));
                int pageCount = pdfDoc.GetNumberOfPages();
                var texts = pdfStamperData.Texts;
                var imgs = pdfStamperData.Imgs;
                var wmimgs = pdfStamperData.WMImgs;
                var wmtxts = pdfStamperData.WMTexts;
                for (int i = 1; i <= pageCount; i++)
                {
                    PdfPage page = pdfDoc.GetPage(i);

                    PdfCanvas canvas = new PdfCanvas(page);
                    if (page.GetRotation() > 0)
                    {
                        switch (page.GetRotation())
                        {
                            case 90:
                                canvas.ConcatMatrix(0.0, 1.0, -1.0, 0.0, page.GetPageSizeWithRotation().GetTop(), 0.0);
                                break;
                            case 180:
                                canvas.ConcatMatrix(-1.0, 0.0, 0.0, -1.0, page.GetPageSizeWithRotation().GetRight(), page.GetPageSizeWithRotation().GetTop());
                                break;
                            case 270:
                                canvas.ConcatMatrix(0.0, -1.0, 1.0, 0.0, 0.0, page.GetPageSizeWithRotation().GetRight());
                                break;
                        }
                    }


                    if (texts.TryGetValue(i, out List<PdfText>? textList))
                    {
                        foreach (PdfText txt in textList)
                        {
                            if (string.IsNullOrEmpty(txt.Uri))
                            {
                                AddText(canvas, txt.Text, txt.Size, txt.X, txt.Y, txt.FontFamily, txt.Color, txt.Rotation, txt.FillOpacity);
                            }
                            else
                            {
                                //width 用  txt.FillOpacity  height 用 txt.StrokeOpacity
                                page.AddAnnotation(AddUri(txt.Uri, txt.X, txt.Y, txt.FillOpacity, txt.StrokeOpacity));
                            }
                        }
                    }
                    if (imgs.TryGetValue(i, out List<PdfImg>? imgList))
                    {
                        foreach (PdfImg img in imgList)
                        {
                            AddImage(canvas, img.Imggb, img.NewWidth, img.NewHeight, img.AbsoluteX, img.AbsoluteY, img.RotationDegrees, img.Opacity);
                        }
                    }

                    //單頁的浮水印
                    if (wmimgs.TryGetValue(i, out List<PdfImgWaterMark>? wmimgList))
                    {
                        foreach (PdfImgWaterMark img in wmimgList)
                        {
                            AddRepeatedImageWatermark(canvas, img.Imggb, img.Size, page.GetPageSize(), img.Rotation, img.Opacity, img.StartX, img.StartY, img.StepX, img.StepY, img.EndX, img.EndY);
                        }
                    }

                    if (wmtxts.TryGetValue(i, out List<PdfTextWaterMark>? wmtxtList))
                    {
                        foreach (PdfTextWaterMark txt in wmtxtList)
                        {
                            AddRepeatedTextWatermark(canvas, txt.Text, txt.Size, page.GetPageSize(), txt.FontFamily, txt.Color, txt.Rotation, txt.Opacity, txt.StartX, txt.StartY, txt.StepX, txt.StepY, txt.EndX, txt.EndY);
                        }
                    }
                    if (wmimgs.TryGetValue(0, out List<PdfImgWaterMark>? allwmimgList))
                    {
                        foreach (PdfImgWaterMark img in allwmimgList)
                        {
                            AddRepeatedImageWatermark(canvas, img.Imggb, img.Size, page.GetPageSize(), img.Rotation, img.Opacity, img.StartX, img.StartY, img.StepX, img.StepY, img.EndX, img.EndY);
                        }
                    }

                    if (wmtxts.TryGetValue(0, out List<PdfTextWaterMark>? allwmtxtList))
                    {
                        foreach (PdfTextWaterMark txt in allwmtxtList)
                        {
                            AddRepeatedTextWatermark(canvas, txt.Text, txt.Size, page.GetPageSize(), txt.FontFamily, txt.Color, txt.Rotation, txt.Opacity, txt.StartX, txt.StartY, txt.StepX, txt.StepY, txt.EndX, txt.EndY);
                        }
                    }

                }
                pdfDoc.Close();
                retVal = outStream.ToArray();
                outStream.Close();
            }
            catch
            {
                throw;
            }
            finally
            {
                pdfDoc?.Close();
                outStream?.Close();
            }
            return retVal;
        }

       
        /// <summary>
        /// 加入附件
        /// </summary>
        /// <param name="pdfFile">PDF檔案</param>
        /// <param name="pdfAttachFile">附件定義,中文檔名不支持</param>
        /// <returns></returns>
        public static byte[] AddFiles(byte[] pdfFile, PdfAttachFile pdfAttachFile)
        {
            byte[] retVal = Array.Empty<byte>();
            MemoryStream outStream = null;
            PdfDocument pdfDoc = null;
            try
            {
                outStream = new MemoryStream();
                pdfDoc = new PdfDocument(new PdfReader(new MemoryStream(pdfFile)), new PdfWriter(outStream));
                foreach (var file in pdfAttachFile.Files)
                {
                    // 創建文件規格並將文件附加到PDF
                    PdfFileSpec spec = PdfFileSpec.CreateEmbeddedFileSpec(pdfDoc, file.Value, file.Key, file.Key, null, null, null);
                    // This method adds file attachment at document level.
                    pdfDoc.AddFileAttachment(file.Key, spec);
                }
                ////測試AddNote
                //pdfDoc.GetFirstPage().AddAnnotation(AddNote("測試Assssssssssssssss", "測試AddNote測試AddNote測試AddNote測試AddNote", 100,100,0,0,0.3));

                pdfDoc.Close();
                retVal = outStream.ToArray();
                outStream.Close();
            }
            catch
            {
                throw;
            }
            finally
            {
                pdfDoc?.Close();
                outStream?.Close();
            }
            return retVal;
        }
		/// <summary>
		/// 移除所有附件
		/// </summary>
		/// <param name="pdfFile">PDF檔案</param>
		/// <returns></returns>
		public static byte[] RemoveFiles(byte[] pdfFile)
        {
            byte[] retVal = Array.Empty<byte>();
            MemoryStream outStream = null;
            PdfDocument pdfDoc = null;
            try
            {
                outStream = new MemoryStream();
                pdfDoc = new PdfDocument(new PdfReader(new MemoryStream(pdfFile)), new PdfWriter(outStream));
                PdfDictionary root = pdfDoc.GetCatalog().GetPdfObject();
                PdfDictionary names = root.GetAsDictionary(PdfName.Names);
                // Remove the whole EmbeddedFiles dictionary from the Names dictionary.
                names.Remove(PdfName.EmbeddedFiles);
                pdfDoc.Close();
                retVal = outStream.ToArray();
                outStream.Close();
            }
            catch
            {
                throw;
            }
            finally
            {
                pdfDoc?.Close();
                outStream?.Close();
            }
            return retVal;
        }
		
		private static PdfLinkAnnotation AddUri(string uri, float x, float y, float w, float h)
        {
            // Create a link annotation
            Rectangle rect = new Rectangle(x, y, w, h);
            PdfLinkAnnotation link = new PdfLinkAnnotation(rect);
            // Create an action (for example, opening a website)
            PdfAction action = PdfAction.CreateURI(uri);
            // Set the action for the link annotation
            link.SetAction(action);
            PdfAnnotationBorder border = new PdfAnnotationBorder(0, 0, 0);
            link.SetBorder(border);
            return link;
        }
        /// <summary>
        /// 加入註解
        /// </summary>
        /// <param name="title">標題</param>
        /// <param name="content">內容</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param
        /// <param name="opacity">不透明度:1~0 </param>
        /// <returns></returns>
        private static PdfAnnotation AddNote(string title, string content, float x, float y, float w = 0, float h = 0, double opacity = 0.3)
        {
            PdfAnnotation ann = new PdfTextAnnotation(new Rectangle(x, y, w, h))
            .SetColor(ColorConstants.GREEN)
            .SetTitle(new PdfString(title, PdfEncodings.UTF8))
            .SetContents(content)
            .SetStrokingOpacity((float)opacity)//不透明度
            .SetFlags(PdfAnnotation.LOCKED | PdfAnnotation.READ_ONLY); //鎖定 和 唯讀

            return ann;
        }
        private static void AddText(PdfCanvas canvas, string text, float fontSize, float x, float y, string fonts = "DFKai-SB", string Color = "#000000", float rotationDegrees = 0, float opacity = 1.0f)
        {
            // Save Now State
            canvas.SaveState();
            // 轉換角度
            canvas.ConcatMatrix(AffineTransform.GetRotateInstance(Math.PI * rotationDegrees / 180, x, y));
            canvas.SetExtGState(new PdfExtGState().SetFillOpacity(opacity));
            //Begin text mode
            canvas.BeginText();

            //Set the font and font size
            string fontPath = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            FontProgramFactory.RegisterFontDirectory(fontPath);
            //canvas.SetFontAndSize(PdfFontFactory.CreateRegisteredFont("Microsoft JhengHei"), fontSize);//正黑體
            if (PdfFontFactory.IsRegistered(fonts))
            {
                canvas.SetFontAndSize(PdfFontFactory.CreateRegisteredFont(fonts), fontSize);
            }
            else
            {
                canvas.SetFontAndSize(PdfFontFactory.CreateFont(StandardFonts.HELVETICA), fontSize);
            }
            canvas.SetLeading(fontSize);

            canvas.SetFillColorRgb((float)Convert.ToInt32(Color.Substring(1, 2), 16) / 255f, (float)Convert.ToInt32(Color.Substring(3, 2), 16) / 255f, (float)Convert.ToInt32(Color.Substring(5, 2), 16) / 255f);

            //canvas.SetFontAndSize(PdfFontFactory.CreateRegisteredFont("Verdana"), fontSize);

            //FontProgramFactory.RegisterSystemFontDirectories(); 
            //canvas.SetFontAndSize(PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN), fontSize);

            //canvas.SetFontAndSize(PdfFontFactory.CreateFont("NotoSansCJKsc-Regular", "UniGB-UCS2-H",EmbeddingStrategy.FORCE_EMBEDDED), fontSize);

            //Set the text color
            //canvas.SetFillColorRgb(0, 0, 0);

            //set xy
            canvas.MoveText(x, y);

            //Add the text to the canvas
            //canvas.ShowTextAligned("Hello World", 100, 100, iText.Layout.Properties.TextAlignment.LEFT);


            var lines = text.Split('\n');  // 使用換行符號分割文字為多行
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                //canvas.NewlineShowText(line);
                if (i == 0) { canvas.ShowText(line); }
                else { canvas.NewlineShowText(line); }
            }

            //End text mode
            canvas.EndText();

            // 恢復轉角度
            canvas.RestoreState();
        }
        private static void AddImage(PdfCanvas canvas, byte[] imgData, float newWidth, float newHeight, float absoluteX, float absoluteY, float rotationDegrees = 0, float opacity = 1.0f)
        {
            float radians = (float)(Math.PI * rotationDegrees / 180);
            float cos = (float)Math.Cos(radians);
            float sin = (float)Math.Sin(radians);

            ImageData img = ImageDataFactory.Create(imgData);

            // 計算需轉換的矩陣
            float a = cos * newWidth;
            float b = sin * newWidth;
            float c = -sin * newHeight;
            float d = cos * newHeight;
            float e = absoluteX;
            float f = absoluteY;
            //透明度
            PdfExtGState gs1 = new PdfExtGState().SetFillOpacity(opacity);
            canvas.SaveState();
            canvas.SetExtGState(gs1);

            // 加入圖像
            canvas.AddImageWithTransformationMatrix(img, a, b, c, d, e, f, false);

            // 恢復旋轉角度
            canvas.RestoreState();

        }


        private static void AddRepeatedTextWatermark(PdfCanvas canvas, string text, float fontSize, Rectangle pageSize, string fonts = "DFKai-SB", string Color = "#000000", float rotationDegrees = 0, float opacity = 1.0f, float startX = 0, float startY = 0, float stepX = 100f, float stepY = 100f, float endX = 0f, float endY = 0f)
        {
            float xStep = stepX;
            float yStep = stepY;

            float xStart = startX;
            float yStart = startY;

            for (float x = xStart; x < pageSize.GetWidth(); x += xStep)
            {
                for (float y = yStart; y < pageSize.GetHeight(); y += yStep)
                {
                    AddText(canvas, text, fontSize, x, y, fonts, Color, rotationDegrees, opacity);
                }
            }
        }

        private static void AddRepeatedImageWatermark(PdfCanvas canvas, byte[] imgData, float size, Rectangle pageSize, float rotationDegrees = 0, float opacity = 1.0f, float startX = 0, float startY = 0, float stepX = 100f, float stepY = 100f, float endX = 0f, float endY = 0f)
        {
            ImageData img = ImageDataFactory.Create(imgData);

            float radians = (float)(Math.PI * rotationDegrees / 180);
            float widthScale = size / img.GetWidth();


            float rotatedWidth = Math.Abs(size * (float)Math.Cos(radians)) + Math.Abs(widthScale * img.GetHeight() * (float)Math.Sin(radians));
            float rotatedHeight = Math.Abs(size * (float)Math.Sin(radians)) + Math.Abs(widthScale * img.GetHeight() * (float)Math.Cos(radians));

            float xStep = rotatedWidth + stepX;
            float yStep = rotatedHeight + stepY;

            float xStart = startX;
            float yStart = startY;

            for (float x = xStart; x < pageSize.GetWidth(); x += xStep)
            {
                for (float y = yStart; y < pageSize.GetHeight(); y += yStep)
                {
                    AddImage(canvas, imgData, size, img.GetHeight() * (size / img.GetWidth()), x, y, rotationDegrees, opacity);
                }
            }
        }
		/// <summary>
		/// 取得頁數
		/// </summary>
		/// <param name="pdfFile">PDF檔案</param>
		/// <returns></returns>
		public static int GetPages(byte[] pdfFile)
        {
            using (MemoryStream input = new MemoryStream(pdfFile))
            using (PdfReader reader = new PdfReader(input))
            using (PdfDocument document = new PdfDocument(reader))
            {
                return document.GetNumberOfPages();
            }
        }
        /// <summary>
        /// 取得PDF每頁大小
        /// </summary>
        /// <param name="pdfFile"></param>
        /// <returns></returns>
        public static List<Tuple<float, float>> GetPageData(byte[] pdfFile)
        {
            List<Tuple<float, float>> returnData = new List<Tuple<float, float>>();
            using (MemoryStream input = new MemoryStream(pdfFile))
            using (PdfReader reader = new PdfReader(input))
            using (PdfDocument document = new PdfDocument(reader))
            {
                for (int i = 1; i <= document.GetNumberOfPages(); i++) 
                {
                    PdfPage page = document.GetPage(i);
                    Rectangle pagesize = page.GetPageSize();
                    returnData.Add(Tuple.Create<float, float>(pagesize.GetWidth(), pagesize.GetHeight()));
                }
            }
            return returnData;
        }
        #endregion

        //#region IText7 PdfOptimizer
        ///// <summary>
        ///// PDF 壓縮 
        ///// 注意!!!!!!!!!!
        ///// 需引用itext.licensing.base
        ///// 載入方法LicenseKey.LoadLicenseFile(new FileInfo(System.IO.Path.Combine(IOHelper.BaseDirectory, "49a3851a95e3fb62bca311993ed1c095d8d3dc6af847af4caa9ae18d8a6310ab.json")));
        ///// </summary>
        ///// <param name="data"></param>
        ///// <param name="pdfOption"></param>
        ///// <param name="func"></param>
        ///// <returns></returns>
        //public static byte[] PdfOptimizer(byte[] data,PdfOption pdfOption, Dictionary<string, Func<byte[], int, byte[]>> func)
        //{
        //    //LicenseKey外面載入
        //    //LicenseKey.LoadLicenseFile(new FileInfo(System.IO.Path.Combine(IOHelper.BaseDirectory, "49a3851a95e3fb62bca311993ed1c095d8d3dc6af847af4caa9ae18d8a6310ab.json")));

        //    PdfOptimizer optimizer = new PdfOptimizer();
        //    //Report Log 先關閉
        //    //FileReportPublisher publisher = new FileReportPublisher(new FileInfo(System.IO.Path.Combine("PdfOptimize", "report.txt")));
        //    //FileReportBuilder builder = new FileReportBuilder(SeverityLevel.INFO, publisher);
        //    //optimizer.SetReportBuilder(builder);
        //    if (pdfOption.GethasFontDuplication())
        //    {
        //        optimizer.AddOptimizationHandler(new FontDuplicationOptimizer());
        //    }
        //    if (pdfOption.GethasFontSubsetting())
        //    {
        //        optimizer.AddOptimizationHandler(new FontSubsettingOptimizer());
        //    }

        //    if (pdfOption.GethasImageQuality())
        //    {
        //        ImageQualityOptimizer tiff_optimizer = new ImageQualityOptimizer();
        //        tiff_optimizer.SetTiffProcessor(new JepunImgCompressor(pdfOption.GetImageQuality(), func["Png"]));
        //        optimizer.AddOptimizationHandler(tiff_optimizer);

        //        ImageQualityOptimizer jpeg_optimizer = new ImageQualityOptimizer();
        //        jpeg_optimizer.SetJpegProcessor(new JepunImgCompressor(pdfOption.GetImageQuality(), func["Jpg"]));
        //        optimizer.AddOptimizationHandler(jpeg_optimizer);

        //        ImageQualityOptimizer png_optimizer = new ImageQualityOptimizer();
        //        png_optimizer.SetPngProcessor(new JepunImgCompressor(pdfOption.GetImageQuality(), func["Png"]));
        //        optimizer.AddOptimizationHandler(png_optimizer);
        //    }
        //    if (pdfOption.GethasCompression())
        //    {
        //        optimizer.AddOptimizationHandler(new CompressionOptimizer());
        //    }
        //    using (MemoryStream reader = new MemoryStream(data))
        //    using (MemoryStream write = new MemoryStream())
        //    {
        //        var datas = optimizer.Optimize(
        //                reader,
        //                write);
        //        byte[] returnData = write.ToArray();
        //        return returnData;
        //    }



        //}

        //#endregion

        #region IText7 SearchText
        /// <summary>
        /// 查詢文字 取得位置和頁數
        /// </summary>
        /// <param name="pdfFile">PDF檔案</param>
        /// <param name="searchText">查詢文字</param>
        /// <returns>(string Text, float StartX, float StartY, float EndX, float EndY, int Page)</returns>
        public static List<PdfTextPosition> SearchText(byte[] pdfFile, string searchText)
        {
            List<PdfTextPosition> result = new ();

            using (MemoryStream input = new MemoryStream(pdfFile))
            using (PdfReader reader = new PdfReader(input))
            using (PdfDocument document = new PdfDocument(reader))
            {
                PdfDocumentContentParser parser = new PdfDocumentContentParser(document);
                //List<string> searchKeywords = new List<string>() { Text };
                for (int pageNumber = 1; pageNumber <= document.GetNumberOfPages(); pageNumber++)
                {
                    // 創建文字搜尋方式
                    FullTextExtractionStrategy strategy = new FullTextExtractionStrategy(searchText);
                    parser.ProcessContent(pageNumber, strategy);
                    // 抓到所有文本所在訊息
                    List<MatchedTextInfo> matchedTextContainers = strategy.GetMatchedTexts();

                    // 處理匹配的文本及其位置資訊
                    foreach (MatchedTextInfo container in matchedTextContainers)
                    {
                        string matchedText = container.Text;
                        Rectangle boundingRectangle = container.Rectangle;
						PdfTextPosition resultdetail = new();
                        resultdetail.Text = container.Text;
                        resultdetail.StartX = boundingRectangle.GetLeft();
                        resultdetail.StartY = boundingRectangle.GetTop();
                        resultdetail.EndX = boundingRectangle.GetX();//boundingRectangle.GetLeft() + boundingRectangle.GetWidth();
                        resultdetail.EndY = boundingRectangle.GetTop() - boundingRectangle.GetHeight();
                        resultdetail.Page = pageNumber;
                        result.Add(resultdetail);
                    }
                }
            }
            return result;
        }
		/// <summary>
		/// 查詢文字 取得位置和頁數 以區塊方式
		/// </summary>
		/// <param name="pdf">PDF檔案</param>
		/// <param name="searchText">查詢文字</param>
		/// <returns>PdfTextPositionChunk</returns>
		public static List<PdfTextPositionChunk> SearchTextChunk(byte[] pdf, string searchText)
        {
            List<PdfTextPositionChunk> result = new ();

            using (MemoryStream input = new MemoryStream(pdf))
            using (PdfReader reader = new PdfReader(input))
            using (PdfDocument document = new PdfDocument(reader))
            {
                for (int page = 1; page <= document.GetNumberOfPages(); page++)
                {
                    var strategy = new ChunkTextExtractionStrategy();
                    var parser = new PdfCanvasProcessor(strategy);
                    parser.ProcessPageContent(document.GetPage(page));


                    strategy.GetResultantText();

                    List<JepunTextInfo> searchResults;
                    if (string.IsNullOrEmpty(searchText))
                    {//關鍵字為空白 直接返回,取得完整 表示要全部取回
                        searchResults = strategy.TextLocationInfos;
                    }
                    else
                    {
                        searchResults = strategy.TextLocationInfos.Where(p => p.Text.Contains(searchText)).OrderBy(p => p.Top).Reverse().ToList();
                        // var searchResults = textLocationInfo.Where(p => p.Text.Contains(searchText)).OrderBy(p => p.GetBaseline().GetBoundingRectange().GetBottom()).Reverse().ToList();
                    }
                    foreach (var searchResult in searchResults)
                    {
						PdfTextPositionChunk resultDetail = new();
                        resultDetail.Text = searchResult.Text;
                        resultDetail.StartX = searchResult.Left;
                        resultDetail.StartY = searchResult.Top;
                        resultDetail.EndX = searchResult.Right;
                        resultDetail.EndY = searchResult.Bottom;
                        resultDetail.Page = page;
                        resultDetail.CharSpaceWidth = searchResult.CharSpaceWidth;
                        resultDetail.SearchText = searchText;
                        //resultDetail.Item1 = searchResult.Text();
                        //resultDetail.Item2 = searchResult.GetBaseline().GetBoundingRectange().GetLeft();
                        //resultDetail.Item3 = searchResult.GetBaseline().GetBoundingRectange().GetTop();
                        //resultDetail.Item4 = searchResult.GetBaseline().GetBoundingRectange().GetRight();
                        //resultDetail.Item5 = searchResult.GetBaseline().GetBoundingRectange().GetBottom();
                        //resultDetail.Item6 = page;
                        //resultDetail.Item7 = searchResult.GetSingleSpaceWidth();
                        //resultDetail.Item8 = searchText;
                        result.Add(resultDetail);
                    }
                }

            }

            return result;
        }
		/// <summary>
		/// 尋找Pdf多個字元位置並插入新的圖片
		/// </summary>
		/// <param name="pdfFile">PDF檔案</param>
		/// <param name="searchTexts">查詢文字</param>
		/// <param name="pdfImgs">影像設定</param>        
		/// <returns>Tuple<byte[], int, string></returns>
		public static Tuple<byte[], int, string> SearchMultiTextAddImgToPdf(byte[] pdfFile, List<string> searchTexts, List<List<PdfImg>> pdfImgs)
        {
            StringBuilder stringBuilder = new StringBuilder();
            List<List<PdfTextPosition>> results = new ();

            foreach (string keyWord in searchTexts)
            {
                // 取得各個關鍵字,所在頁數與位置
                List<PdfTextPosition> tmp = SearchText(pdfFile, keyWord);
                if (tmp.Count == 0)
                {
                    stringBuilder.Append($"找不到該關鍵字{keyWord}");
                    continue;
                }
                results.Add(tmp);
            }
            if (!string.IsNullOrEmpty(stringBuilder.ToString()))
            {
                Console.WriteLine(stringBuilder.ToString());
                //return Tuple.Create<byte[], int, string>(pdfFile, 0, stringBuilder.ToString());
            }
            using (var input = new MemoryStream(pdfFile))
            using (var output = new MemoryStream())
            {
                int pageCount = 0;
                using (var pdfDoc = new PdfDocument(new PdfReader(input), new PdfWriter(output)))
                {
                    pageCount = pdfDoc.GetNumberOfPages();
                    for (int i = 1; i <= pageCount; i++)
                    {
                        PdfPage page = pdfDoc.GetPage(i);
                        PdfCanvas canvas = new PdfCanvas(page);
                        if (page.GetRotation() > 0)
                        {
                            switch (page.GetRotation())
                            {
                                case 90:
                                    canvas.ConcatMatrix(0.0, 1.0, -1.0, 0.0, page.GetPageSizeWithRotation().GetTop(), 0.0);
                                    break;
                                case 180:
                                    canvas.ConcatMatrix(-1.0, 0.0, 0.0, -1.0, page.GetPageSizeWithRotation().GetRight(), page.GetPageSizeWithRotation().GetTop());
                                    break;
                                case 270:
                                    canvas.ConcatMatrix(0.0, -1.0, 1.0, 0.0, 0.0, page.GetPageSizeWithRotation().GetRight());
                                    break;
                            }
                        }
                        for (int j = 0; j < results.Count(); j++)
                        {// 各個關鍵字,所在頁數與位置 集合
                            for (int k = 0; k < results[j].Count(); k++)
                            {// 所在頁數與位置
								PdfTextPosition result = results[j][k];
                                if (result.Page == i)// 確認 關鍵字 的頁數
                                {
                                    for (int l = 0; l < pdfImgs[j].Count(); l++)
                                    {
                                        float absoluteX = result.EndX + pdfImgs[j][l].AbsoluteX;
                                        float absoluteY = result.EndY + pdfImgs[j][l].AbsoluteY;
                                        AddImage(canvas, pdfImgs[j][l].Imggb, pdfImgs[j][l].NewWidth, pdfImgs[j][l].NewHeight, absoluteX, absoluteY);
                                    }
                                }
                            }
                        }
                    }
                }
                return Tuple.Create<byte[], int, string>(output.ToArray(), pageCount, stringBuilder.ToString());
            }
        }

		/// <summary>
		/// 尋找Pdf多個字元位置並插入新的圖片 ,但X的位置是固定傳入
		/// </summary>
		/// <param name="pdfFile">PDF檔案</param>
		/// <param name="searchTexts">查詢文字</param>
		/// <param name="startXs">X的位置是固定傳入</param>
		/// <param name="pdfImgs">影像設定</param>
		/// <returns>Tuple<byte[], int></returns>
		public static Tuple<byte[], int> SearchMultiTextChunkAddImgToPdf(byte[] pdfFile, List<string> searchTexts, List<float> startXs, List<List<PdfImg>> pdfImgs)
        {
            List<List<PdfTextPositionChunk>> results = new List<List<PdfTextPositionChunk>>();
            foreach (string keyWord in searchTexts)
            {
                // 取得各個關鍵字,所在頁數與位置
                results.Add(SearchTextChunk(pdfFile, keyWord));
            }
            using (var input = new MemoryStream(pdfFile))
            using (var output = new MemoryStream())
            {
                int pageCount = 0;
                using (var pdfDoc = new PdfDocument(new PdfReader(input), new PdfWriter(output)))
                {
                    pageCount = pdfDoc.GetNumberOfPages();
                    for (int i = 1; i <= pageCount; i++)
                    {
                        PdfPage page = pdfDoc.GetPage(i);
                        PdfCanvas canvas = new PdfCanvas(page);
                        if (page.GetRotation() > 0)
                        {
                            switch (page.GetRotation())
                            {
                                case 90:
                                    canvas.ConcatMatrix(0.0, 1.0, -1.0, 0.0, page.GetPageSizeWithRotation().GetTop(), 0.0);
                                    break;
                                case 180:
                                    canvas.ConcatMatrix(-1.0, 0.0, 0.0, -1.0, page.GetPageSizeWithRotation().GetRight(), page.GetPageSizeWithRotation().GetTop());
                                    break;
                                case 270:
                                    canvas.ConcatMatrix(0.0, -1.0, 1.0, 0.0, 0.0, page.GetPageSizeWithRotation().GetRight());
                                    break;
                            }
                        }
                        for (int j = 0; j < results.Count(); j++)
                        {// 各個關鍵字,所在頁數與位置 集合
                            for (int k = 0; k < results[j].Count(); k++)
                            {// 所在頁數與位置
								PdfTextPositionChunk result = results[j][k];
                                if (result.Page == i)// 確認 關鍵字 的頁數
                                {
                                    for (int l = 0; l < pdfImgs[j].Count(); l++)
                                    {
                                        float absoluteX = startXs[j] + pdfImgs[j][l].AbsoluteX;
                                        float absoluteY = result.EndY + pdfImgs[j][l].AbsoluteY;
                                        AddImage(canvas, pdfImgs[j][l].Imggb, pdfImgs[j][l].NewWidth, pdfImgs[j][l].NewHeight, absoluteX, absoluteY);
                                    }
                                }
                            }
                        }
                    }
                }
                return Tuple.Create<byte[], int>(output.ToArray(), pageCount);
            }
        }
		#endregion

		#region IText7 GetText
		/// <summary>
		/// 每頁 返回有 /n
		/// </summary>
		/// <param name="pdfFile">PDF檔案</param>
		/// <returns>Dictionary<int, string></returns>
		public static Dictionary<int, string> GetText(byte[] pdfFile)
        {
            Dictionary<int, string> extractedTextDict = new Dictionary<int, string>();
            using (MemoryStream input = new MemoryStream(pdfFile))
            using (PdfReader reader = new PdfReader(input))
            using (PdfDocument document = new PdfDocument(reader))
            {
                //斷行
                // Loop through all the pages and extract text
                for (int page = 1; page <= document.GetNumberOfPages(); page++)
                {

                    //ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                    //LocationTextExtractionStrategy  出來 排序比 SimpleTextExtractionStrategy 好
                    LocationTextExtractionStrategy strategy = new();//同一行順序 Top 相同

                    PdfPage pdfPage = document.GetPage(page);

                    //效果一樣
                    string data = PdfTextExtractor.GetTextFromPage(pdfPage, strategy);
                    //Console.WriteLine(strategy.GetResultantText());
                    extractedTextDict.Add(page, data);

                    //PdfCanvasProcessor parser = new PdfCanvasProcessor(strategy);
                    //parser.ProcessPageContent(pdfPage);
                    //string extractedText = strategy.GetResultantText();
                    //extractedTextDict.Add(page, extractedText);					 

                }

                //不斷行 + 空白
                //for (int page = 1; page <= document.GetNumberOfPages(); page++)
                //{
                //    var strategy = new CustomTextEventListener();
                //    PdfPage pdfPage = document.GetPage(page);
                //    PdfCanvasProcessor parser = new PdfCanvasProcessor(strategy);
                //    parser.ProcessPageContent(pdfPage);
                //    string extractedText = strategy.GetResultantText();
                //    extractedTextDict.Add(page, extractedText.Replace(" ", ""));
                //    extractedTextDict.Add(page, extractedText);
                //}

                //
                //for (int page = 1; page <= document.GetNumberOfPages(); page++)
                //{
                //    var strategy = new ChunkTextExtractionStrategy();
                //    var parser = new PdfCanvasProcessor(strategy);
                //    parser.ProcessPageContent(document.GetPage(page));

                //    List<JepunTextInfo> textLocationInfo = strategy.TextLocationInfo;
                //    //strategy.GetResultantText();
                //    string extractedText = strategy.GetResultantText();
                //    extractedTextDict.Add(page, extractedText);
                //}

            }

            return extractedTextDict;
        }

		/// <summary>
		/// 每頁 返回一行
		/// </summary>
		/// <param name="pdfFile">PDF檔案</param>
		/// <returns>Dictionary<int, string></returns>
		public static Dictionary<int, string> GetTextLine(byte[] pdfFile)
        {
            Dictionary<int, string> extractedTextDict = new Dictionary<int, string>();
            using (MemoryStream input = new MemoryStream(pdfFile))
            using (PdfReader reader = new PdfReader(input))
            using (PdfDocument document = new PdfDocument(reader))
            {
                //不斷行 + 空白
                for (int page = 1; page <= document.GetNumberOfPages(); page++)
                {

                    var strategy = new CustomTextEventListener();//區塊順序
                    PdfPage pdfPage = document.GetPage(page);
                    PdfCanvasProcessor parser = new PdfCanvasProcessor(strategy);
                    parser.ProcessPageContent(pdfPage);
                    string extractedText = strategy.GetResultantText();
                    extractedTextDict.Add(page, extractedText);

                }
            }

            return extractedTextDict;
        }

		/// <summary>
		/// 區塊,每頁 返回有 /n
		/// </summary>
		/// <param name="pdfFile">PDF檔案</param>
		/// <returns>Dictionary<int, string></returns>
		public static Dictionary<int, string> GetTextChunk(byte[] pdfFile)
        {
            Dictionary<int, string> extractedTextDict = new Dictionary<int, string>();
            using (MemoryStream input = new MemoryStream(pdfFile))
            using (PdfReader reader = new PdfReader(input))
            using (PdfDocument document = new PdfDocument(reader))
            {
                for (int page = 1; page <= document.GetNumberOfPages(); page++)
                {
                    if (page == 19)
                    {
                        Console.WriteLine(19);
                    }
                    var strategy = new ChunkTextExtractionStrategy();
                    var parser = new PdfCanvasProcessor(strategy);
                    parser.ProcessPageContent(document.GetPage(page));

                    List<JepunTextInfo> textLocationInfo = strategy.TextLocationInfos;
                    //strategy.GetResultantText();
                    string extractedText = strategy.GetResultantText();
                    extractedTextDict.Add(page, extractedText);
                }
            }
            return extractedTextDict;
        }

		/// <summary>
		/// 使用 SearchText 方法取得 
		/// </summary>
		/// <param name="pdfFile">PDF檔案</param>
		/// <returns>List<(string Text, float StartX, float StartY, float EndX, float EndY, int Page)></returns>
		public static List<PdfTextPosition> GetTextFullList(byte[] pdfFile)
        {
            List<PdfTextPosition> tmp = SearchText(pdfFile, "");
            return tmp;
        }
		/// <summary>
		/// 使用 SearchTextChunk 方法取得 
		/// </summary>
		/// <param name="pdfFile">PDF檔案</param>
		/// <returns>List<(string Text, float StartX, float StartY, float EndX, float EndY, int Page, float CharSpaceWidth, string SearchText)></returns>
		public static List<PdfTextPositionChunk> GetTextChunkList(byte[] pdfFile)
        {
            List<PdfTextPositionChunk> tmp = SearchTextChunk(pdfFile, "");
            return tmp;
        }




        #endregion

        #region IText7 Html to PDF
        /// <summary>
        /// 將網頁轉換成PDF
        /// </summary>
        /// <param name="html">網頁</param>
        /// <returns>byte</returns>
        public static byte[] HtmlToPdf(string html)
        {
            byte[] bytes;
            using (var memoryStream = new MemoryStream())
            {
                ConverterProperties converterProperties = new ConverterProperties();
                HtmlConverter.ConvertToPdf(html, memoryStream, converterProperties);
                bytes = memoryStream.ToArray();
            }
            return bytes;
        }
        /// <summary>
        /// 將網頁轉換成PDF
        /// </summary>
        /// <param name="htmlStream">串流</param>
        /// <returns>byte</returns>
        public static byte[] HtmlToPdf(Stream htmlStream)
        {
            byte[] bytes;
            using (var memoryStream = new MemoryStream())
            {
                ConverterProperties converterProperties = new ConverterProperties();
                HtmlConverter.ConvertToPdf(htmlStream, memoryStream, converterProperties);
                bytes = memoryStream.ToArray();
            }
            return bytes;
        }
		#endregion
		#region IText7 Signature  
		/// <summary>
		/// 驗證 IText 簽章
		/// </summary>
		/// <param name="pdfFile">PDF檔案</param>
		/// <returns>Dictionary<string,bool></returns>
		public static Dictionary<string,bool> VerifySignatures(byte[] pdfFile)
        {
			Dictionary<string, bool> verifySignatures = new Dictionary<string, bool>();
			using (MemoryStream input = new MemoryStream(pdfFile))
            using (PdfReader reader = new PdfReader(input))
            using (PdfDocument pdfDoc = new PdfDocument(reader))
            {
                PdfAcroForm form = PdfAcroForm.GetAcroForm(pdfDoc, false);
                // 獲取所有的簽章名稱
                SignatureUtil signUtil = new SignatureUtil(pdfDoc);
                IList<string> signatureNames = signUtil.GetSignatureNames();
                foreach (string name in signatureNames)
                {
                    Console.WriteLine($"簽章名稱: {name}");
                    // 驗證簽章是否覆蓋了全部文件內容
                    bool isFullDocumentCovered = signUtil.SignatureCoversWholeDocument(name);
                    Console.WriteLine($"簽章覆蓋了整個文件: {isFullDocumentCovered}");
                    // 獲取簽章資訊
                    PdfPKCS7 pkcs7 = signUtil.ReadSignatureData(name);
                    bool isSignatureValid = pkcs7.VerifySignatureIntegrityAndAuthenticity();
                    Console.WriteLine($"簽章是否有效: {isSignatureValid}");
                    // 獲取簽署者的憑證
                    foreach (IX509Certificate cert in pkcs7.GetCertificates())
                    {
                        Console.WriteLine($"簽署者: {cert.GetSubjectDN}");
                    }
                    Console.WriteLine("---------");
					verifySignatures.Add(name, isSignatureValid);
				}
            }
            return verifySignatures;
		}
		/// <summary>
		/// 簽章
		/// </summary>
		/// <param name="pdfFile">PDF檔案</param>
		/// <param name="pfxFilePath">pfx檔案path</param>
		/// <param name="pfxPassword">pwd</param>
		/// <param name="signatureFieldName">名字</param>		
		/// <param name="reason">原因</param>
		/// <param name="location">地點</param>
		/// <param name="x">區塊 x</param>
		/// <param name="y">區塊 y</param>
		/// <param name="width">區塊  寬</param>
		/// <param name="height">區塊 高</param>
		/// <param name="page">顯示簽名區塊 頁位置</param>
		/// <param name="signPicture">簽名圖檔,如簽名軌跡</param>
		/// <returns>byte[]</returns>
		public static byte[] SignPdf(byte[] pdfFile, string pfxFilePath, string pfxPassword, string signatureFieldName, string reason = "",string location = "", float x = 0, float y = 0, float width = 0, float height = 0, int page = 1, byte[] signPicture = null)
		{
            // 加載 PFX 憑證8.0.5
			Pkcs12Store pk12 = new Pkcs12StoreBuilder().Build();
			//取得私鑰和證書鏈
			pk12.Load(new FileStream(pfxFilePath, FileMode.Open, FileAccess.Read), pfxPassword.ToCharArray());
			string alias = "";
			foreach (var a in pk12.Aliases)
			{
				alias = ((string)a);
				if (pk12.IsKeyEntry(alias))
					break;
			}
			ICipherParameters pk = pk12.GetKey(alias).Key;
			X509CertificateEntry[] ce = pk12.GetCertificateChain(alias);
			X509Certificate[] chain = new X509Certificate[ce.Length];
			for (int k = 0; k < ce.Length; ++k)
			{
				chain[k] = ce[k].Certificate;
			}
			IX509Certificate[] certificateWrappers = new IX509Certificate[chain.Length];
			for (int i = 0; i < certificateWrappers.Length; ++i)
			{
				certificateWrappers[i] = new X509CertificateBC(chain[i]);
			}			
			using (MemoryStream input = new MemoryStream(pdfFile))
			using (PdfReader reader = new PdfReader(input))
			using (MemoryStream output = new MemoryStream())
			{
				StampingProperties sp = new StampingProperties();
				PdfSigner signer = new PdfSigner(reader, output, sp.UseAppendMode());
				// 設置簽章名稱
				signer.SetReason(reason);
				signer.SetLocation(location);
				signer.SetFieldName(signatureFieldName);
				if (height != 0)
                {					
					signer.SetPageNumber(page);
					signer.SetPageRect(new Rectangle(x, y, width, height));
				}				
                if(signPicture != null)
                {
					SignatureFieldAppearance appearance = new SignatureFieldAppearance("app");					 
					appearance.SetContent(new SignedAppearanceText(), ImageDataFactory.Create(signPicture));
					signer.SetSignatureAppearance(appearance);
				}
				// 使用 BouncyCastle 簽章
				IExternalSignature pks = new PrivateKeySignature(new PrivateKeyBC(pk), DigestAlgorithms.SHA256);
				signer.SignDetached(pks, certificateWrappers, null, null, null, 0, PdfSigner.CryptoStandard.CMS);
				return output.ToArray();
			}			 
		}

		#endregion


		#region IText7 Properties
		/// <summary>
		/// 添加屬性
		/// </summary>
		/// <param name="pdfFile">PDF檔案</param>
		/// <param name="customProps">屬性</param>
		/// <returns></returns>
		public static byte[] AddCustomProperty(byte[] pdfFile,Dictionary<string,string> customProps)
        {
            byte[] retVal = Array.Empty<byte>();
            MemoryStream outStream = null;
            PdfDocument pdfDoc = null;
            try
            {
                outStream = new MemoryStream();
                pdfDoc = new PdfDocument(new PdfReader(new MemoryStream(pdfFile)), new PdfWriter(outStream));

                // 取得 XMP Metadata（元數據）
                PdfDocumentInfo info = pdfDoc.GetDocumentInfo();
                // 添加自定義屬性
                foreach (string propertyName in customProps.Keys)
                {
                    info.SetMoreInfo(propertyName, customProps[propertyName]);
                }              

                // 若需要，還可以設定標準的 PDF 元數據屬性，如標題、作者等
                //info.SetTitle("My Custom PDF");
                //info.SetAuthor("Author Name");
                pdfDoc.Close();
                retVal = outStream.ToArray();
                outStream.Close();
            }
            catch
            {
                throw;
            }
            finally
            {
                pdfDoc?.Close();
                outStream?.Close();
            }
            return retVal;







        }


        #endregion
    }
}
