using DocuSign.CodeExamples.Authentication;
using DocuSign.eSign.Client;
using static DocuSign.eSign.Client.Auth.OAuth;
using static DocuSign.eSign.Client.Auth.OAuth.UserInfo;
using eSignature.Examples;
using System;
using System.Diagnostics;
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web;
using System.IO;
using System.Collections.Generic;

namespace DocuSign.CodeExamples.JWT_Console
{
    class Program
    {
        static readonly string DevCenterPage = "https://developers.docusign.com/platform/auth/consent";

        static void Main(string[] args)
        {
            string base64BinaryStr = "";
            byte[] bytes = Convert.FromBase64String(base64BinaryStr);
            File.WriteAllBytes(@"D:\danie\Downloads\pdfFileName.pdf", bytes);
            System.IO.FileStream stream = new FileStream(@"D:\danie\Downloads\file.pdf", FileMode.CreateNew);
            System.IO.BinaryWriter writer =
                new BinaryWriter(stream);
            writer.Write(bytes, 0, bytes.Length);
            writer.Close();



            Console.ForegroundColor = ConsoleColor.White;
            OAuthToken accessToken = null;
            try
            {
                accessToken = JWTAuth.AuthenticateWithJWT("ESignature", ConfigurationManager.AppSettings["ClientId"], ConfigurationManager.AppSettings["ImpersonatedUserId"],
                                                            ConfigurationManager.AppSettings["AuthServer"], ConfigurationManager.AppSettings["PrivateKeyFile"]);
            }
            catch (ApiException apiExp)
            {
                // Consent for impersonation must be obtained to use JWT Grant
                if (apiExp.Message.Contains("consent_required"))
                {
                    // Caret needed for escaping & in windows URL
                    string caret = "";
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        caret = "^";
                    }

                    // build a URL to provide consent for this Integration Key and this userId
                    string url = "https://" + ConfigurationManager.AppSettings["AuthServer"] + "/oauth/auth?response_type=code" + caret + "&scope=impersonation%20signature" + caret +
                        "&client_id=" + ConfigurationManager.AppSettings["ClientId"] + caret + "&redirect_uri=" + DevCenterPage;
                    Console.WriteLine($"Consent is required - launching browser (URL is {url})");

                    // Start new browser window for login and consent to this app by DocuSign user
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = false });
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        Process.Start("xdg-open", url);
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        Process.Start("open", url);
                    }

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Unable to send envelope; Exiting. Please rerun the console app once consent was provided");
                    Console.ForegroundColor = ConsoleColor.White;
                    Environment.Exit(-1);
                }
            }

            var apiClient = new ApiClient();
            apiClient.SetOAuthBasePath(ConfigurationManager.AppSettings["AuthServer"]);
            UserInfo userInfo = apiClient.GetUserInfo(accessToken.access_token);
            Account acct = userInfo.Accounts.FirstOrDefault();

            eSign.Model.EnvelopesInformation resultsEnvolpe = SigningViaEmail.GetListEnvelopeDate(accessToken.access_token, acct.BaseUri + "/restapi", acct.AccountId, 30);

            eSign.Model.EnvelopeDocumentsResult listaDocumentos = new eSign.Model.EnvelopeDocumentsResult();

            foreach (var result in resultsEnvolpe.Envelopes)
            {
                List<eSign.Model.EnvelopeDocument> envolopeDocument = new List<eSign.Model.EnvelopeDocument>();

                listaDocumentos = SigningViaEmail.GetListDocumentEnvelope(accessToken.access_token, acct.BaseUri + "/restapi", acct.AccountId, result.EnvelopeId);

                envolopeDocument.Add(listaDocumentos.EnvelopeDocuments.FirstOrDefault());

                var fileStream = SigningViaEmail.GetListDocument(accessToken.access_token, acct.BaseUri + "/restapi", acct.AccountId,
                                                result.EnvelopeId, listaDocumentos.EnvelopeDocuments.FirstOrDefault().DocumentId);

                using (Stream file = File.Create("donwloadDocuSign.pdf"))
                {
                    CopyStream(fileStream, file);
                }

            }



            Console.WriteLine("Bem - vindo ao exemplo de código JWT! ");
            Console.Write("Digite o endereço de e - mail do signatário: ");
            string signerEmail = Console.ReadLine();
            Console.Write("Digite o nome do signatário: ");
            string signerName = Console.ReadLine();
            Console.Write("Digite o endereço de e-mail da cópia carbono: ");
            string ccEmail = Console.ReadLine();
            Console.Write("Digite o nome da cópia carbono: ");
            string ccName = Console.ReadLine();
            string docDocx = Path.Combine(@"..", "..", "..", "..", "launcher-csharp", "World_Wide_Corp_salary.docx");
            //string docPdf = Path.Combine(@"..", "..", "..", "..", "launcher-csharp", "World_Wide_Corp_lorem.pdf");
            string docPdf = Path.Combine(@"D:\danie\Downloads", "AutorizacaoFaturamentoCDC_contrato_92359.pdf");
            Console.WriteLine("");
            string envelopeId = SigningViaEmail.SendEnvelopeViaEmail(signerEmail, signerName, ccEmail, ccName, accessToken.access_token, acct.BaseUri + "/restapi", acct.AccountId, docDocx, docPdf, "sent");
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Envelope enviado com sucesso com envelopeId {envelopeId}");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.White;
            Environment.Exit(0);
        }

        public static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[8 * 1024];
            int len;
            while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, len);
            }
        }
    }
}
