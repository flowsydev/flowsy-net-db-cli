using Flowsy.Db.Cli.Resources;
using Flowsy.Db.Sql;
using Flowsy.Db.Sql.Migrations;
using Microsoft.Extensions.Configuration;
using Sharprompt;

namespace Flowsy.Db.Cli;

public sealed class DbPrompt
{
    private readonly IConfiguration? _configuration;
    private readonly string? _configurationSectionName;
    private readonly IEnumerable<string> _positiveAnswers = new[]
    {
        Strings.BooleanAnswerYesLong.ToLower(),
        Strings.BooleanAnswerYesShort.ToLower()
    };

    public DbPrompt(IConfiguration? configuration = null, string? sectionName = null)
    {
        _configuration = configuration;
        _configurationSectionName = sectionName;
    }

    private bool IsPositiveAnswer(string? answer)
        => !string.IsNullOrEmpty(answer) && _positiveAnswers.Contains(answer.ToLower());

    private bool IsNegativeAnswer(string? answer) => !IsPositiveAnswer(answer);

    public DbConfigurationSource SelectConfigurationSource()
        => Prompt.Select<DbConfigurationSource>(options =>
        {
            options.Message = Strings.ConfigurationSource;
            options.Items = Enum.GetValues(typeof(DbConfigurationSource)).Cast<DbConfigurationSource>();
            options.TextSelector = source => source switch
            {
                DbConfigurationSource.Environment => Strings.ConfigurationSourceEnvironment,
                DbConfigurationSource.Manual => Strings.ConfigurationSourceManual,
                _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
            };
        });
    
    public IEnumerable<DbConnectionConfiguration> SelectConnectionConfigurations()
    {
        var configurationSource = SelectConfigurationSource();
        Console.WriteLine();
        
        switch (configurationSource)
        {
            case DbConfigurationSource.Environment:
            {
                if (_configuration is null)
                    throw new InvalidOperationException(Strings.NoConfigurationWasProvided);
                
                return _configuration.GetConnectionConfigurations(_configurationSectionName);
            }
            
            case DbConfigurationSource.Manual:
            {
                var configurations = new List<DbConnectionConfiguration>();

                var count = 0;
                while (true)
                {
                    Console.WriteLine(Strings.ConfigurationNumberX, ++count);
                    
                    var connectionKey = Prompt.Input<string>(Strings.EnterConnectionKey, placeholder: "Default, Main, Staging");

                    var provider = Prompt.Input<string>(Strings.ProviderInvariantName);
                    if (!DbProvider.IsKnown(provider))
                        throw new InvalidOperationException(string.Format(Strings.UnknownDatabaseProviderX, provider));
                    
                    var host = Prompt.Input<string>(Strings.HostOrIpAddress, "localhost");
                    var port = Prompt.Input<int>(Strings.Port, DbProvider.GetDefaultPort(provider));
                    var database = Prompt.Input<string>(Strings.DatabaseName, DbProvider.GetDefaultDatabase(provider));
                    var user = Prompt.Input<string>(Strings.User, DbProvider.GetDefaultUser(provider));
                    var password = Prompt.Password(Strings.Password);
                    var additionalOptions = Prompt.Input<string>(Strings.AdditionalOptions);

                    var connectionString = DbProvider.BuildConnectionString(host, port, database, user, password, additionalOptions);

                    DbMigrationConfiguration? migrationConfiguration = null;
                    if (Prompt.Confirm(Strings.ConfigureMigrations))
                    {
                        var sourceDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Migrations", connectionKey); 
                        sourceDirectory = Prompt.Input<string>(Strings.DirectoryContainigMigrationScripts, sourceDirectory);
                        
                        var metadataTableSchema = Prompt.Input<string>(Strings.SchemaContainingMigrationMetadata);
                        var metadataTableName = Prompt.Input<string>(Strings.TableContainingMigrationMetadata);
                        var initializationStatement = Prompt.Input<string>(Strings.SqlStatementToBeExecutedAfterMigrations);

                        migrationConfiguration =
                            new DbMigrationConfiguration(sourceDirectory, metadataTableSchema, metadataTableName, initializationStatement);
                    }
                    
                    configurations.Add(new DbConnectionConfiguration(connectionKey, provider, connectionString)
                    {
                        Migration = migrationConfiguration
                    });
                    
                    if (!Prompt.Confirm(Strings.ConfigureAnotherConnection))
                        break;
                    
                    Console.WriteLine();
                }
                
                return configurations;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public async Task RunMigrationsAsync(CancellationToken cancellationToken)
    {
        var configurations = SelectConnectionConfigurations();
        var dbManager = new DbManager(configurations, Console.WriteLine);

        await dbManager.MigrateAsync(cancellationToken);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var operation = Prompt.Select<DbOperation>(options =>
        {
            options.Message = Strings.ChooseDatabaseOperation;
            options.Items = Enum.GetValues(typeof(DbOperation)).Cast<DbOperation>();
            options.TextSelector = op => op switch
            {
                DbOperation.Migration => Strings.RunMigrations,
                _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
            };
        });

        switch (operation)
        {
            case DbOperation.Migration:
                await RunMigrationsAsync(cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}