using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

class Program
{
    public class Empresa
    {
        public string Nome { get; set; }
        public string Fantasia { get; set; }
        public string Cnpj { get; set; }
        public string Situacao { get; set; }
        public string Tipo { get; set; }
        public string Abertura { get; set; }
        public string Natureza_juridica { get; set; }
        public string Logradouro { get; set; }
        public string Numero { get; set; }
        public string Bairro { get; set; }
        public string Municipio { get; set; }
        public string Uf { get; set; }
        public string Cep { get; set; }
    }

    static async Task Main(string[] args)
    {
        Console.Write("Digite um CNPJ (somente números): ");
        string cnpj = Console.ReadLine()?.Trim();

        if (!Regex.IsMatch(cnpj, @"^\d{14}$"))
        {
            Console.WriteLine("CNPJ inválido. Deve conter exatamente 14 números (sem pontos, traços ou barras).");
            return;
        }

        try
        {
            Empresa empresa = await ConsultarCnpjComTratamentoAsync(cnpj);

            if (empresa?.Nome == null)
            {
                Console.WriteLine("CNPJ não encontrado ou dados indisponíveis.");
                return;
            }

            Console.WriteLine("\nEmpresa encontrada:");
            Console.WriteLine($"Nome: {empresa.Nome}");
            Console.WriteLine($"Fantasia: {empresa.Fantasia}");
            Console.WriteLine($"Situação: {empresa.Situacao}");
            Console.WriteLine($"Abertura: {empresa.Abertura}");
            Console.WriteLine($"Endereço: {empresa.Logradouro}, {empresa.Numero} - {empresa.Bairro}, {empresa.Municipio}/{empresa.Uf} - CEP: {empresa.Cep}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro ao consultar o CNPJ:");
            Console.WriteLine(ex.Message);
        }
    }

    static async Task<Empresa> ConsultarCnpjComTratamentoAsync(string cnpj)
    {
        string url = $"https://www.receitaws.com.br/v1/cnpj/{cnpj}";
        var client = new HttpClient();

        client.DefaultRequestHeaders.Add("User-Agent", "ConsultaCnpjApp");

        int tentativas = 0;
        const int maxTentativas = 5;

        while (tentativas < maxTentativas)
        {
            tentativas++;

            HttpResponseMessage response = await client.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                Console.WriteLine($"⚠️  Limite de requisições excedido. Aguardando 60 segundos (tentativa {tentativas}/{maxTentativas}):");

                for (int i = 60; i >= 0; i--)
                {
                    Console.Write($"\rAguardando... {i}s  ");
                    await Task.Delay(1000);
                }

                Console.WriteLine("\n🔁 Tentando novamente...\n");
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Erro HTTP: {(int)response.StatusCode} - {response.ReasonPhrase}");
            }

            string json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            return JsonSerializer.Deserialize<Empresa>(json, options);
        }

        throw new Exception("Número máximo de tentativas excedido após erros de limite de requisição.");
    }
}
