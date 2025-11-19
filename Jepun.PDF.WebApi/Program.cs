using iText.Kernel.Pdf;
using Jepun.Core.Pdf;
using Jepun.Core.Pdf.Model;
using Jepun.PDF.WebApi.Common;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Web;
using System.Net;
using System.Reflection;

namespace Jepun.PDF.WebApi
{
	public class Program
	{
		public static void Main(string[] args)
		{
			// Early init of NLog to allow startup and exception logging, before host is built
			var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
			logger.Info("init main");
			
			var builder = WebApplication.CreateBuilder(args);		 

			try
			{
				var config = (new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json")).Build();
				string serverPort = config["ServerPort"] + "";
				string pfxFilePath = config["PfxFilePath"] + "";
				string pfxPassword = config["PfxPassword"] + "";

				
				builder.Services.Configure< Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
				{//更改回傳Json時為大駝峰命名
					options.SerializerOptions.PropertyNamingPolicy = null;
				});
				// NLog: Setup NLog for Dependency injection https://github.com/NLog/NLog/wiki/Getting-started-with-ASP.NET-Core-6
				builder.Logging.ClearProviders();
				builder.Host.UseNLog();
				//只紀錄請求的資料，但不記錄body，以免參數也記錄進去
				builder.Services.AddHttpLogging(logging =>
				{
					logging.LoggingFields = HttpLoggingFields.Request;
					//logging.RequestBodyLogLimit = 0;
				});
				//有使用憑證
				builder.Services.AddHsts(options =>
				{
					options.Preload = true;
					options.IncludeSubDomains = true;
					options.MaxAge = TimeSpan.FromSeconds(31536000);
					//options.ExcludedHosts.Add("example.com");   //要排除的主機清單
					//options.ExcludedHosts.Add("www.example.com");//要排除的主機清單
				});
				//配合   app.UseHttpsRedirection();//設定永久重新導向,強制將非 http 轉至  https
				builder.Services.AddHttpsRedirection(options =>
				{
					options.RedirectStatusCode = (int)HttpStatusCode.TemporaryRedirect;
					options.HttpsPort = int.Parse(serverPort);
				});

				//https://127.0.0.1:5566/swagger/index.html
				// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
				builder.Services.AddEndpointsApiExplorer();
				builder.Services.AddSwaggerGen(c =>
				{
					c.SwaggerDoc("v1", new OpenApiInfo
					{
						Title = "Jepun.Line.WebApi",
						Version = "v1",
						Description = "A .NET 8.0  PDF Web API",
						Contact = new OpenApiContact
						{
							Name = "PDF",
							Email = string.Empty
						},
						License = new OpenApiLicense
						{
							Name = "Use under MIT",
						}
					});
					// Set the comments path for the Swagger JSON and UI.
					var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
					var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
					c.IncludeXmlComments(xmlPath);
				});
				
				var app = builder.Build();


				// Configure the HTTP request pipeline.
				app.UseHsts();//HTTP 嚴格傳輸安全性通訊協定 (HSTS),UseHsts 不建議在開發中，因為 HSTS 設定可由瀏覽器進行高度快取。 根據預設， UseHsts 會排除本機回送位址。
				app.UseHttpsRedirection(); //使用https
				app.UseHttpLogging();				 
				
				app.UseSwagger();
				app.UseSwaggerUI(options =>
				{
					string swaggerJsonBasePath = string.IsNullOrWhiteSpace(options.RoutePrefix) ? "." : "..";
					options.SwaggerEndpoint($"{swaggerJsonBasePath}/swagger/v1/swagger.json", "PDF API V1");
				});

				app.MapGet("/", () => "Hello World!");
				#region PDF
				
				app.MapPost("/isProtected", async (HttpContext context) =>
				{
					bool result = false;
					try
					{				
						var pdf = await Program.GetBytes(context);
						result = PdfHelper.IsProtected(pdf);
					}
					catch (Exception ex)
					{
						logger.Error(ex, ex.Message);
					}
					return TypedResults.Ok(result?"1":"0");
				}).DisableAntiforgery();

                app.MapPost("/decryptPDF", async (HttpContext context, [FromForm] string password) => {
                    try
                    {
                        var pdf = await Program.GetBytes(context);
                        byte[] bytes = PdfHelper.DecryptPDF(pdf, password);
                        await Program.ReturnBytes(context, bytes);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, ex.Message);
                    }
                }).DisableAntiforgery();


                app.MapPost("/encryptPDF", async (HttpContext context,[FromForm] string userPwd, [FromForm] string strength, [FromForm] string owrPwd, [FromForm] string pmss) => {
					try
					{						 
						var pdf = await Program.GetBytes(context);
						byte[] bytes = PdfHelper.EncryptPDF(pdf, userPwd, strength == "false" ? false:true, owrPwd,int.Parse(pmss));
						await Program.ReturnBytes(context, bytes);
					}
					catch (Exception ex)
					{
						logger.Error(ex, ex.Message);
					}
				}).DisableAntiforgery();
				
				 
				app.MapPost("/addStamper", async (HttpContext context,[FromForm] string jsonPdfStamperData) => {
					try
					{
						PdfStamperData pdfStamperData = jsonPdfStamperData.JsonToObj<PdfStamperData>();
						var pdf = await Program.GetBytes(context);
						byte[] bytes = PdfHelper.AddStamper(pdf, pdfStamperData);
						await Program.ReturnBytes(context, bytes);


						//context.Response.ContentType = "application/octet-stream";
						//context.Response.Headers.Add("Content-Disposition", "attachment; filename=\"addStamper.pdf\"");
						//await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);

						//IFormFile file = context.Request.Form.Files[0];						 
						//using (var memoryStream = new MemoryStream())
						//{
						//	await file.CopyToAsync(memoryStream);
						//	byte[]  bytes = PdfHelper.AddStamper(memoryStream.ToArray(), pdfStamperData);
						//	//File.WriteAllBytes(Path.Combine("d:\\", "output_AddStamper.Pdf"), bytes);
						//	// 設置回應的內容類型，"application/octet-stream" 表示通用的二進位數據流
						//	// 這裡可以更改為具體的 MIME 類型，例如 image/jpeg、application/pdf 等
						//	context.Response.ContentType = "application/octet-stream";
						//	// 設置回應標頭來指定檔案名稱
						//	context.Response.Headers.Add("Content-Disposition", "attachment; filename=\"addStamper.pdf\"");
						//	// 回傳 byte[] 資料
						//	await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
						//}						
					}
					catch (Exception ex)
					{
						logger.Error(ex,ex.Message);
					}
				}).DisableAntiforgery();
				
			 
				app.MapPost("/addFiles", async (HttpContext context, [FromForm] string jsonPdfAttachFile) => {
					try
					{
						PdfAttachFile pdfAttachFile = jsonPdfAttachFile.JsonToObj<PdfAttachFile>();
						var pdf = await Program.GetBytes(context);
						byte[] bytes = PdfHelper.AddFiles(pdf, pdfAttachFile);
						await Program.ReturnBytes(context, bytes);
					}
					catch (Exception ex)
					{
						logger.Error(ex, ex.Message);
					}
				}).DisableAntiforgery();
				
				app.MapPost("/removeFiles", async (HttpContext context) => {
					try
					{
						var pdf = await Program.GetBytes(context);
						byte[] bytes = PdfHelper.RemoveFiles(pdf);
						await Program.ReturnBytes(context, bytes);
					}
					catch (Exception ex)
					{
						logger.Error(ex, ex.Message);
					}
				}).DisableAntiforgery();
				
				app.MapPost("/getPages", async (HttpContext context) =>
				{
					int result = 0;
					try
					{
						var pdf = await Program.GetBytes(context);
						result = PdfHelper.GetPages(pdf);
					}
					catch (Exception ex)
					{
						logger.Error(ex, ex.Message);
					}
					return TypedResults.Ok(result);
				}).DisableAntiforgery();

                app.MapPost("/getPageData", async (HttpContext context) =>
                {
                    List<Tuple<float, float>> result = new();
                    try
                    {
                        var pdf = await Program.GetBytes(context);
                        result = PdfHelper.GetPageData(pdf);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, ex.Message);
                    }
                    return TypedResults.Ok(result);
                }).DisableAntiforgery();
                #endregion
                #region IText7 SearchText

                app.MapPost("/searchText", async (HttpContext context, [FromForm] string searchText) => {
					List<PdfTextPosition> result = new();
					try
					{
						var pdf = await Program.GetBytes(context);
						result = PdfHelper.SearchText(pdf, searchText);						
					}
					catch (Exception ex)
					{
						logger.Error(ex, ex.Message);
					}
					 
					return TypedResults.Ok(result);
				}).DisableAntiforgery();
				
				app.MapPost("/searchTextChunk", async (HttpContext context, [FromForm] string searchText) => {
					List<PdfTextPositionChunk> result = new List<PdfTextPositionChunk>();
					try
					{
						var pdf = await Program.GetBytes(context);
						result = PdfHelper.SearchTextChunk(pdf, searchText);
					}
					catch (Exception ex)
					{
						logger.Error(ex, ex.Message);
					}
					return TypedResults.Ok(result);
				}).DisableAntiforgery();
				
				app.MapPost("/searchMultiTextAddImgToPdf", async (HttpContext context, [FromForm] string jsonSearchTexts, [FromForm] string jsonPdfImgs) => {
					Tuple<byte[], int, string> result = Tuple.Create<byte[], int, string>(null,0,"");
					try
					{
						List<string> searchTexts = jsonSearchTexts.JsonToObj<List<string>>();
						List<List<PdfImg>> pdfImgs = jsonPdfImgs.JsonToObj<List<List<PdfImg>>>();
						var pdf = await Program.GetBytes(context);
						result = PdfHelper.SearchMultiTextAddImgToPdf(pdf, searchTexts, pdfImgs);
					}
					catch (Exception ex)
					{
						logger.Error(ex, ex.Message);
					}
					return TypedResults.Ok(result);
				}).DisableAntiforgery();
				
				app.MapPost("/searchMultiTextChunkAddImgToPdf", async (HttpContext context, [FromForm] string jsonSearchTexts, [FromForm] string jsonStartXs, [FromForm] string jsonPdfImgs) => {
					Tuple<byte[], int> result = Tuple.Create<byte[], int>(null, 0);
					try
					{
						List<string> searchTexts = jsonSearchTexts.JsonToObj<List<string>>();
						List<float> startXs = jsonStartXs.JsonToObj<List<float>>();
						List<List<PdfImg>> pdfImgs = jsonPdfImgs.JsonToObj<List<List<PdfImg>>>();
						var pdf = await Program.GetBytes(context);
						result = PdfHelper.SearchMultiTextChunkAddImgToPdf(pdf, searchTexts, startXs, pdfImgs);
					}
					catch (Exception ex)
					{
						logger.Error(ex, ex.Message);
					}
					return TypedResults.Ok(result);
				}).DisableAntiforgery();
				/// <summary>
				/// 每頁 返回有 /n
				/// </summary>
				/// <param name="pdf">PDF檔案</param>
				/// <returns>Dictionary<int, string></returns>
				app.MapPost("/getText", async (HttpContext context) => {
					Dictionary<int, string> result = new();
					try
					{						 
						var pdf = await Program.GetBytes(context);
						result = PdfHelper.GetText(pdf);
					}
					catch (Exception ex)
					{
						logger.Error(ex, ex.Message);
					}
					return TypedResults.Ok(result);
				}).DisableAntiforgery();
				
				app.MapPost("/getTextLine", async (HttpContext context) => {
					Dictionary<int, string> result = new();
					try
					{
						var pdf = await Program.GetBytes(context);
						result = PdfHelper.GetTextLine(pdf);
					}
					catch (Exception ex)
					{
						logger.Error(ex, ex.Message);
					}
					return TypedResults.Ok(result);
				}).DisableAntiforgery();
				
				app.MapPost("/getTextChunk", async (HttpContext context) => {
					Dictionary<int, string> result = new();
					try
					{
						var pdf = await Program.GetBytes(context);
						result = PdfHelper.GetTextChunk(pdf);
					}
					catch (Exception ex)
					{
						logger.Error(ex, ex.Message);
					}
					return TypedResults.Ok(result);
				}).DisableAntiforgery();
				
				app.MapPost("/getTextFullList", async (HttpContext context) => {
					List<PdfTextPosition> result = new();
					try
					{
						var pdf = await Program.GetBytes(context);
						result = PdfHelper.GetTextFullList(pdf);
					}
					catch (Exception ex)
					{
						logger.Error(ex, ex.Message);
					}
					return TypedResults.Ok(result);
				}).DisableAntiforgery();
				
				app.MapPost("/getTextChunkList", async (HttpContext context) => {
					List<PdfTextPositionChunk> result = new();
					try
					{
						var pdf = await Program.GetBytes(context);
						result = PdfHelper.GetTextChunkList(pdf);
					}
					catch (Exception ex)
					{
						logger.Error(ex, ex.Message);
					}
					return TypedResults.Ok(result);
				}).DisableAntiforgery();
				#endregion
				#region IText7 Signature  
				app.MapPost("/verifySignatures", async (HttpContext context) => {
					Dictionary<string, bool> result = new();
					try
					{
						var pdf = await Program.GetBytes(context);
						result = PdfHelper.VerifySignatures(pdf);
					}
					catch (Exception ex)
					{
						logger.Error(ex, ex.Message);
					}
					return TypedResults.Ok(result);
				}).DisableAntiforgery();


				app.MapPost("/signPdf", async (HttpContext context, [FromForm] string signatureFieldName, [FromForm] string reason, [FromForm] string location, [FromForm] string x , [FromForm] string y , [FromForm] string width , [FromForm] string height , [FromForm] string page ) => {
					try
					{
						byte[] pdf, signPicture;
						var pdfs = await Program.GetBytesList(context);
						pdf = pdfs[0];
						signPicture = pdfs.Count == 2? pdfs[1]:null;						 
						byte[] bytes = PdfHelper.SignPdf(pdf, pfxFilePath, pfxPassword, signatureFieldName,reason, location,float.Parse(x), float.Parse(y), float.Parse(width), float.Parse(height),int.Parse(page), signPicture);
						await Program.ReturnBytes(context, bytes);
					}
					catch (Exception ex)
					{
						logger.Error(ex, ex.Message);
					}
				}).DisableAntiforgery();
				#endregion
				#region IText7 Properties

				app.MapPost("/addCustomProperty", async (HttpContext context, [FromForm] string jsonCustomProps) => {
					try
					{
						Dictionary<string, string> customProps = jsonCustomProps.JsonToObj<Dictionary<string, string>>();
						var pdf = await Program.GetBytes(context);
						byte[] bytes = PdfHelper.AddCustomProperty(pdf, customProps);
						await Program.ReturnBytes(context, bytes);
					}
					catch (Exception ex)
					{
						logger.Error(ex, ex.Message);
					}
				}).DisableAntiforgery();

                #endregion
                //app.Run($"https://0.0.0.0:{serverPort}");
                app.Run();
            }
			catch (Exception ex)
			{				// NLog: catch setup errors
				logger.Error(ex, "Stopped Jepun.PDF.WebApi because of exception");
				throw;
			}
			finally
			{
				// Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
				NLog.LogManager.Shutdown();
			}
		}
		
		/// <summary>
		/// 取得 PDF 的 bytes
		/// </summary>
		/// <param name="context">HttpContext</param>
		/// <returns>PDF 的 bytes</returns>
		protected static async Task<byte[]> GetBytes(HttpContext context)
		{
			byte[] bytes;
			IFormFile file = context.Request.Form.Files[0];
			using (var memoryStream = new MemoryStream())
			{
				await file.CopyToAsync(memoryStream);
				bytes = memoryStream.ToArray();				 
			}
			return bytes;
		}
		protected static async Task<List<byte[]>> GetBytesList(HttpContext context)
		{
			List<byte[]> bytes = new List<byte[]>();

			foreach(IFormFile file in context.Request.Form.Files)
			{
				using (var memoryStream = new MemoryStream())
				{
					await file.CopyToAsync(memoryStream);
					bytes.Add( memoryStream.ToArray());
				}
			}			 
			return bytes;
		}
		protected static async Task ReturnBytes(HttpContext context, byte[] bytes)
		{
			context.Response.ContentType = "application/octet-stream";
			context.Response.Headers.Add("Content-Disposition", "attachment; filename=\"doc.pdf\"");
			await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
		}

		
	}
}
