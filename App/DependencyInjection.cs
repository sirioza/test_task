using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StringsGenerator;
using StringsGenerator.Workers;
using StringsSorter;
using StringsSorter.Services;
using StringsSorter.Services.Implementation;
using System.Threading.Channels;

namespace App
{
    public static class DependencyInjection
    {
        public static IServiceCollection ConfigureApp(this IServiceCollection services)
        {
            // option
            // might reconsider the approach with Option
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false)
                .Build();

            services.Configure<StringsGenerator.Options>(configuration.GetSection("StringsGeneratorOptions"));

            // channel
            services.AddSingleton(provider =>
            {
                var options = provider.GetRequiredService<IOptions<StringsGenerator.Options>>().Value;

                return Channel.CreateBounded<(byte[] buffer, int count)>(
                    new BoundedChannelOptions(options.Workers * 8)
                    {
                        SingleWriter = false,
                        SingleReader = true,
                        FullMode = BoundedChannelFullMode.Wait
                    });
            });

            // generator
            services.AddTransient(sp =>
            {
                var сhannel = sp.GetRequiredService<Channel<(byte[] buffer, int count)>>();
                var options = sp.GetRequiredService<IOptions<StringsGenerator.Options>>();
                return new Consumer(сhannel, options);
            });
            services.AddTransient(sp =>
            {
                var сhannel = sp.GetRequiredService<Channel<(byte[] buffer, int count)>>();
                var options = sp.GetRequiredService<IOptions<StringsGenerator.Options>>();
                return new Producer(сhannel, options);
            });
            services.AddTransient(sp =>
            {
                var сhannel = sp.GetRequiredService<Channel<(byte[] buffer, int count)>>();
                var consumer = sp.GetRequiredService<Consumer>();
                var producer = sp.GetRequiredService<Producer>();
                var options = sp.GetRequiredService<IOptions<StringsGenerator.Options>>();
                return new Generator(сhannel, consumer, producer, options);
            });

            // sorter
            services.Configure<StringsSorter.Options>(configuration.GetSection("StringsSorterOptions"));
            services.AddSingleton<IMerger, KWayMerger>();
            services.AddTransient<Sorter>();

            return services;
        }
    }
}
