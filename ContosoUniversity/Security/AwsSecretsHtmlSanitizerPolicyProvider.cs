using System;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Loads the HtmlSanitizer allow-list policy from AWS Secrets Manager.
    /// The secret id is supplied via the HTML_SANITIZER_SECRET_ID environment variable.
    /// Falls back to a restrictive default policy when the secret is not configured/available.
    /// </summary>
    public static class AwsSecretsHtmlSanitizerPolicyProvider
    {
        public const string SecretIdEnvironmentVariable = "HTML_SANITIZER_SECRET_ID";

        public static async Task<HtmlSanitizerPolicy> LoadAsync()
        {
            var secretId = Environment.GetEnvironmentVariable(SecretIdEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(secretId))
            {
                return new HtmlSanitizerPolicy();
            }

            try
            {
                using (var client = new AmazonSecretsManagerClient())
                {
                    var response = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretId });
                    var policy = JsonSerializer.Deserialize<HtmlSanitizerPolicy>(
                        response.SecretString,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return policy ?? new HtmlSanitizerPolicy();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("{\"level\":\"warn\",\"message\":\"Unable to load HtmlSanitizer policy from AWS Secrets Manager; using restrictive defaults\",\"error\":\"" + ex.GetType().Name + "\"}");
                return new HtmlSanitizerPolicy();
            }
        }
    }
}
