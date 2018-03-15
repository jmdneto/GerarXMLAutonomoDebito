using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace GerarXMLAutonomo
{
    class Program
    {
        static void Main(string[] args)
        {
            string fileName = string.Empty, fileNameExport = string.Empty;
            bool isFileValid = false;
            string idImobiliaria = string.Empty, cnpjAutonomo = string.Empty, digValid = "WASSA_";
            string opcao = null;
            //string idImobiliaria = "1034", cnpjAutonomo = "17232427000118";
            //string digValid = "COR2_";

            #region Configura XMLWriter
            var xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.OmitXmlDeclaration = true;
            xmlWriterSettings.Indent = true;
            xmlWriterSettings.NewLineOnAttributes = false;
            #endregion

            while (!isFileValid)


            {
                Console.Write("Digite o tipo de XML a ser gerado: [1] ComandoDebito / [2] InclusãoAutonomo: ");
                opcao = Console.ReadLine().ToString();
                Console.WriteLine("");

                if (opcao != "1" && opcao != "2")
                    Console.Write("Opção inválida!");
                else
                {
                    Console.Write("Digite o código da imobiliaria (ID Imobiliaria): ");
                    idImobiliaria = Console.ReadLine();
                    Console.WriteLine("");

                    if (opcao == "1")
                    {
                        Console.Write("Digite o CNPJ do Autonomo (apenas números, sem pontos e/ou traços): ");
                        cnpjAutonomo = Console.ReadLine().Replace(".", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(" ", "").Trim();
                        Console.WriteLine("");
                    }

                    Console.Write("Digite uma sigla para concatenação do Identificador (pois caso haja identificadores duplicados no sistema ele não processa a leitura do arquivo) [PADRÃO: WASSA]: ");
                    digValid = Console.ReadLine();
                    digValid = (string.IsNullOrWhiteSpace(digValid) ? "WASSA" : digValid)+"_";
                    Console.WriteLine("");

                    Console.Write("Digite o caminho do arquivo original (Completo, ex.: C:\\Temp\\ArquivoOriginal.txt): ");
                    fileName = Console.ReadLine();

                    Console.WriteLine("");
                    Console.Write("Carregando... ");

                    if (!File.Exists(fileName)) Console.Write("Arquivo não existe!");
                    else if (Path.GetExtension(fileName).ToLower() == ".txt" || Path.GetExtension(fileName).ToLower() == ".csv") { isFileValid = true; break; }
                    else Console.Write("Não é um arquivo TXT ou CSV!");
                }

                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine("Pressione qualquer tecla para tentar novamente...");
                Console.ReadKey();
                Console.Clear();
            }

            if (opcao == "1")
            {
                var fileLines = System.IO.File.ReadAllLines(fileName);
                List<ComandoDebito> linhasArquivo = new List<ComandoDebito>();

                foreach (var line in fileLines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";;;")) continue;

                    ComandoDebito comando = new ComandoDebito();
                    comando.CodigoBanco = line.Substring(0, 3).Trim();
                    comando.NumAgencia = line.Substring(24, 4).Trim();
                    comando.DigAgencia = line.Substring(29, 1).Trim();
                    comando.NumConta = line.Substring(36, 5).Trim();
                    comando.DigConta = line.Substring(42, 1).Trim();
                    comando.Nome = line.Substring(43, 30).Trim();
                    comando.CPF = line.Substring(219, 11).Trim();
                    comando.IdRemessa = line.Substring(73, 6).Trim();

                    try { comando.DataPagamento = DateTime.ParseExact(line.Substring(93, 8), "ddMMyyyy", null); }
                    catch { comando.DataPagamento = DateTime.ParseExact(line.Substring(93, 8), "yyyyMMdd", null); }

                    comando.Valor = line.Substring(124, 10).Trim().TrimStart('0');
                    comando.NomeEmpreendimento = line.Substring(196, 16).Trim();

                    linhasArquivo.Add(comando);
                }

                fileNameExport = fileName.Replace(Path.GetExtension(fileName), ".xml");
                using (XmlWriter writer = XmlWriter.Create(fileNameExport, xmlWriterSettings))
                {
                    writer.WriteStartElement("envio");
                    writer.WriteAttributeString("xmlns", "xsd", null, "http://www.w3.org/2001/XMLSchema");
                    writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
                    {
                        writer.WriteStartElement("header");
                        {
                            writer.WriteElementString("numeroVersao", "1.0");
                            writer.WriteElementString("idImobiliaria", idImobiliaria);
                        }
                        writer.WriteEndElement();

                        writer.WriteStartElement("comandos");
                        {
                            writer.WriteStartElement("debitos");
                            {

                                foreach (ComandoDebito item in linhasArquivo)
                                {
                                    writer.WriteStartElement("comandoDebito");
                                    {
                                        writer.WriteElementString("identificadorComandoImobiliaria", digValid + item.DataPagamentoString + item.IdRemessa);
                                        writer.WriteStartElement("identificadores");
                                        {
                                            writer.WriteElementString("identificadorVenda", digValid + "V" + item.IdRemessa);
                                            writer.WriteElementString("identificadorPagamento", digValid + "P" + item.IdRemessa);
                                        }
                                        writer.WriteEndElement();

                                        writer.WriteStartElement("dadosProduto");
                                        {
                                            writer.WriteElementString("nomeEmpreendimento", item.NomeEmpreendimento);
                                            writer.WriteElementString("nomeDivisao", item.NomeEmpreendimento);
                                            writer.WriteElementString("numeroUnidade", "11");
                                            writer.WriteElementString("dataVenda", item.DataPagamentoString);
                                        }
                                        writer.WriteEndElement();

                                        writer.WriteElementString("valor", item.Valor);
                                        writer.WriteElementString("dataAgendamentoPagamento", item.DataPagamentoString);

                                        writer.WriteStartElement("cliente");
                                        {
                                            writer.WriteElementString("nomeCompleto", item.Nome);
                                            writer.WriteElementString("cpf", item.CPF);

                                            writer.WriteStartElement("enderecos");
                                            {
                                                writer.WriteStartElement("endereco");
                                                {
                                                    writer.WriteAttributeString("tipo", "PRINCIPAL");
                                                    writer.WriteElementString("rua", "Avenida Brigadeiro Faria Lima");
                                                    writer.WriteElementString("numero", "2010");
                                                    writer.WriteElementString("complemento", "");
                                                    writer.WriteElementString("bairro", "Jardim Paulista");
                                                    writer.WriteElementString("municipio", "São Paulo");
                                                    writer.WriteElementString("cep", "06712000");
                                                    writer.WriteElementString("uf", "SP");
                                                }
                                                writer.WriteEndElement();
                                            }
                                            writer.WriteEndElement();

                                            writer.WriteStartElement("contatos");
                                            {
                                                writer.WriteStartElement("contato");
                                                {
                                                    writer.WriteAttributeString("tipo", "PRINCIPAL");
                                                    writer.WriteElementString("telefone", "");
                                                    writer.WriteElementString("celular", "");
                                                    writer.WriteElementString("email", "");
                                                }
                                                writer.WriteEndElement();
                                            }
                                            writer.WriteEndElement();
                                        }
                                        writer.WriteEndElement();

                                        writer.WriteStartElement("formaPagamento");
                                        {
                                            writer.WriteAttributeString("tipo", "DEBITOAUTOMATICO");
                                            writer.WriteElementString("numeroBanco", item.CodigoBanco);
                                            writer.WriteElementString("numeroAgencia", item.NumAgencia);
                                            writer.WriteElementString("digitoVerificadorAgencia", item.DigAgencia);
                                            writer.WriteElementString("numeroConta", item.NumConta);
                                            writer.WriteElementString("digitoVerificadorConta", item.DigConta);
                                        }
                                        writer.WriteEndElement();

                                        writer.WriteStartElement("autonomos");
                                        {
                                            writer.WriteStartElement("autonomo");
                                            {
                                                writer.WriteElementString("cpf", cnpjAutonomo);
                                                writer.WriteElementString("valor", item.Valor);
                                            }
                                            writer.WriteEndElement();
                                        }
                                        writer.WriteEndElement();
                                    }
                                    writer.WriteEndElement();
                                }
                            }
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    writer.Flush();
                }

                Console.WriteLine("Arquivo gerado com sucesso!");
                Console.ReadKey();
            }
            else if (opcao == "2")
            {
                String dataProcessamento = DateTime.Now.ToString("yyyyMMdd");
                var fileLines = System.IO.File.ReadAllLines(fileName, Encoding.Default);
                List<ComandoInclusaoAutonomo> linhasArquivo = new List<ComandoInclusaoAutonomo>();

                foreach (var line in fileLines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";;;")) continue;

                    ComandoInclusaoAutonomo comando = new ComandoInclusaoAutonomo();
                    List<String> linhaseparada = line.Split(';').ToList();


                    comando.NomeCompleto = linhaseparada[0].Trim();
                    comando.Cpf = linhaseparada[1].Replace(".", "").Replace("-", "").Trim();
                    comando.DataNascimento = "19900101";
                    comando.Sexo = "M";
                    comando.Rg = "0000000";
                    comando.OrgaoExpedidorRg = "SSP".Replace(".", "").Replace("-", "").Trim();
                    comando.Rua = linhaseparada[4].Trim();
                    comando.Numero = linhaseparada[5].Trim();
                    comando.Complemento = linhaseparada[6].Trim();
                    comando.Bairro = linhaseparada[7].Trim();
                    comando.Municipio = linhaseparada[8].Trim();
                    comando.Uf = linhaseparada[9].Trim();
                    comando.Cep = linhaseparada[10].Replace(".", "").Replace("-", "").Trim();
                    comando.Telefone = linhaseparada[12].Replace(".", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(" ", "").Trim();
                    comando.Celular = linhaseparada[12].Replace(".", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(" ", "").Trim();
                    comando.Email = linhaseparada[11].Trim();

                    //comando.NomeCompleto = linhaseparada[0].Trim();
                    //comando.Cpf = linhaseparada[1].Replace(".", "").Replace("-", "").Trim();
                    //comando.DataNascimento = "19900101";
                    //comando.Sexo = "M";
                    //comando.Rg = linhaseparada[2].Trim();
                    //comando.OrgaoExpedidorRg = "SSP".Replace(".", "").Replace("-", "").Trim();
                    //comando.Rua = linhaseparada[4].Trim();
                    //comando.Numero = linhaseparada[5].Trim();
                    //comando.Complemento = linhaseparada[6].Trim();
                    //comando.Bairro = linhaseparada[7].Trim();
                    //comando.Municipio = linhaseparada[8].Trim();
                    //comando.Uf = linhaseparada[9].Trim();
                    //comando.Cep = linhaseparada[10].Replace(".", "").Replace("-", "").Trim();
                    //comando.Telefone = linhaseparada[12].Replace(".", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(" ", "").Trim();
                    //comando.Celular = linhaseparada[12].Replace(".", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(" ", "").Trim();
                    //comando.Email = linhaseparada[11].Trim();

                    //comando.NomeCompleto = linhaseparada[0].Trim();
                    //comando.Cpf = linhaseparada[1].Replace(".", "").Replace("-", "").Trim();
                    //comando.DataNascimento = !string.IsNullOrWhiteSpace(linhaseparada[5]) ? linhaseparada[5].Replace("/", "").Trim() : "01011990";
                    //comando.Sexo = linhaseparada[6].Trim();
                    //comando.Rg = linhaseparada[2].Trim();
                    //comando.OrgaoExpedidorRg = "SSP".Replace(".", "").Replace("-", "").Trim();
                    //comando.Rua = linhaseparada[7].Trim();
                    //comando.Numero = linhaseparada[8].Trim();
                    //comando.Complemento = linhaseparada[9].Trim();
                    //comando.Bairro = linhaseparada[10].Trim();
                    //comando.Municipio = linhaseparada[11].Trim();
                    //comando.Uf = linhaseparada[12].Trim();
                    //comando.Cep = linhaseparada[13].Replace(".", "").Replace("-", "").Trim();
                    //comando.Telefone = linhaseparada[4].Replace(".", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(" ", "").Trim();
                    //comando.Celular = linhaseparada[3].Replace(".", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(" ", "").Trim();
                    //comando.Email = linhaseparada[15].Trim();

                    linhasArquivo.Add(comando);
                }

                linhasArquivo.RemoveAt(0);
                int autoincremento = 1;
                fileNameExport = fileName.Replace(Path.GetExtension(fileName), ".xml");
                using (XmlWriter writer = XmlWriter.Create(fileNameExport, xmlWriterSettings))
                {
                    writer.WriteStartElement("envio");
                    writer.WriteAttributeString("xmlns", "xsd", null, "http://www.w3.org/2001/XMLSchema");
                    writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
                    {
                        writer.WriteStartElement("header");
                        {
                            writer.WriteElementString("numeroVersao", "1.0");
                            writer.WriteElementString("idImobiliaria", idImobiliaria);
                        }
                        writer.WriteEndElement();

                        writer.WriteStartElement("comandos");
                        {
                            writer.WriteStartElement("movimentacoesAutonomos");
                            {

                                foreach (ComandoInclusaoAutonomo item in linhasArquivo)
                                {
                                    writer.WriteStartElement("movimentacaoAutonomo");

                                    {
                                        writer.WriteAttributeString("enviarSenha", "2");
                                        writer.WriteElementString("identificadorComandoImobiliaria", digValid + idImobiliaria + dataProcessamento + "_" + autoincremento);
                                        writer.WriteElementString("cpf", item.Cpf);
                                        writer.WriteElementString("nomeCompleto", item.NomeCompleto);
                                        writer.WriteElementString("dataNascimento", item.DataNascimento);
                                        writer.WriteElementString("sexo", item.Sexo);
                                        writer.WriteElementString("rg", item.Rg);
                                        writer.WriteElementString("orgaoExpedidorRg", item.OrgaoExpedidorRg);

                                        writer.WriteStartElement("enderecos");
                                        {
                                            writer.WriteStartElement("endereco");
                                            {
                                                writer.WriteAttributeString("tipo", "PRINCIPAL");
                                                writer.WriteElementString("rua", item.Rua);
                                                writer.WriteElementString("numero", item.Numero);
                                                writer.WriteElementString("complemento", item.Complemento);
                                                writer.WriteElementString("bairro", item.Bairro);
                                                writer.WriteElementString("municipio", item.Municipio);
                                                writer.WriteElementString("cep", item.Cep);
                                                writer.WriteElementString("uf", item.Uf);
                                            }
                                            writer.WriteEndElement();
                                        }
                                        writer.WriteEndElement();

                                        writer.WriteStartElement("contatos");
                                        {
                                            writer.WriteStartElement("contato");
                                            {
                                                writer.WriteAttributeString("tipo", "PRINCIPAL");
                                                writer.WriteElementString("telefone", item.Telefone);
                                                writer.WriteElementString("celular", item.Celular);
                                                writer.WriteElementString("email", item.Email);
                                            }
                                            writer.WriteEndElement();
                                        }
                                        writer.WriteEndElement();
                                    }
                                    writer.WriteEndElement();
                                    autoincremento++;
                                }
                            }
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    writer.Flush();
                }

                Console.WriteLine("Arquivo gerado com sucesso!");
                Console.ReadKey();

            }
        }
    }

    public class ComandoDebito
    {
        public string CodigoBanco { get; set; }
        public string NumAgencia { get; set; }
        public string DigAgencia { get; set; }
        public string NumConta { get; set; }
        public string DigConta { get; set; }
        public string Nome { get; set; }
        public string CPF { get; set; }
        public string IdRemessa { get; set; }
        public DateTime DataPagamento { get; set; }
        public string DataPagamentoString { get { return DataPagamento.ToString("yyyyMMdd"); } }
        public string Valor { get; set; }
        public string NomeEmpreendimento { get; set; }
    }

    public class ComandoInclusaoAutonomo
    {
        public string Cpf { get; set; }
        public string NomeCompleto { get; set; }
        public string DataNascimento { get; set; }
        public string Sexo { get; set; }
        public string Rg { get; set; }
        public string OrgaoExpedidorRg { get; set; }
        public string EnderecoTipo { get; set; }
        public string Rua { get; set; }
        public string Numero { get; set; }
        public string Complemento { get; set; }
        public string Bairro { get; set; }
        public string Municipio { get; set; }
        public string Cep { get; set; }
        public string Uf { get; set; }
        public string ContatoTipo { get; set; }
        public string Telefone { get; set; }
        public string Celular { get; set; }
        public string Email { get; set; }
        public string Contato { get; set; }
        public DateTime DataProcessamento { get; set; }
        public string DataProcessamentoString { get { return DataProcessamento.ToString("yyyyMMdd"); } }
    }
}
