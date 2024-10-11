using System.IdentityModel.Tokens.Jwt;
using AutoMapper;
using CAVerifierServer.Grain.Tests.GuardianIdentifier;
using CAVerifierServer.Grain.Tests.ThirdParty;
using CAVerifierServer.Grains;
using CAVerifierServer.Grains.Grain;
using CAVerifierServer.Grains.Grain.ThirdPartyVerification;
using CAVerifierServer.Grains.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Hosting;
using Orleans.TestingHost;
using Volo.Abp;
using Volo.Abp.AutoMapper;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ExceptionHandling;
using Volo.Abp.Json;
using Volo.Abp.Json.SystemTextJson;
using Volo.Abp.MultiTenancy;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Threading;
using Volo.Abp.Timing;
using Volo.Abp.Uow;

namespace CAVerifierServer.Grain.Tests;

public class ClusterFixture : IDisposable, ISingletonDependency
{
    public ClusterFixture()
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
        builder.AddClientBuilderConfigurator<TestClientBuilderConfigurator>();
        Cluster = builder.Build();
        Cluster.Deploy();
    }

    public void Dispose()
    {
        Cluster.StopAllSilos();
    }

    public TestCluster Cluster { get; private set; }

    private class TestSiloConfigurations : ISiloConfigurator
    {
        public void Configure(ISiloBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices(services =>
                {
                    services.AddSingleton<IGuardianIdentifierVerificationGrain, GuardianIdentifierVerificationGrain>();
                    services.AddSingleton<IThirdPartyVerificationGrain, ThirdPartyVerificationGrain>();
                    services.AddSingleton<ITelegramAuthProvider, TelegramAuthProvider>();
                    services.Configure<VerifierCodeOptions>(o =>
                    {
                        o.RetryTimes = 1;
                        o.CodeExpireTime = 2;
                        o.GetCodeFrequencyLimit = 1;
                        o.GetCodeFrequencyTimeLimit = 1;
                    });

                    services.Configure<VerifierAccountOptions>(o =>
                    {
                        o.Address = "XXXXX";
                        o.PrivateKey = "XXXXX";
                    });

                    var dic = new Dictionary<string, int>
                    {
                        ["Email"] = 0,
                        ["Phone"] = 1
                    };
                    services.Configure<GuardianTypeOptions>(o => { o.GuardianTypeDic = dic; });
                    services.AddSingleton(new JwtSecurityTokenHandler());

                    services.AddMemoryCache();
                    services.AddDistributedMemoryCache();
                    services.AddSingleton(JwtMock.GetJwtSecurityTokenHandlerMock());
                    services.AddAutoMapper(typeof(CAVerifierServerGrainsModule).Assembly);

                    services.AddSingleton(typeof(DistributedCache<>));
                    services.AddSingleton(typeof(IDistributedCache<>), typeof(DistributedCache<>));
                    services.AddSingleton(typeof(IDistributedCache<,>), typeof(DistributedCache<,>));
                    services.Configure<AbpDistributedCacheOptions>(cacheOptions =>
                    {
                        cacheOptions.GlobalCacheEntryOptions.SlidingExpiration = TimeSpan.FromMinutes(20);
                    });
                    services.AddSingleton<ICancellationTokenProvider>(NullCancellationTokenProvider.Instance);
                    services.AddTransient(
                        typeof(IDistributedCacheSerializer),
                        typeof(Utf8JsonDistributedCacheSerializer)
                    );
                    services.AddTransient(
                        typeof(IJsonSerializer),
                        typeof(AbpSystemTextJsonSerializer)
                    );
                    services.AddTransient(
                        typeof(IDistributedCacheKeyNormalizer),
                        typeof(DistributedCacheKeyNormalizer)
                    );
                    
                    services.AddTransient(typeof(AsyncLocalCurrentTenantAccessor));
                    services.AddTransient(
                        typeof(ICurrentTenantAccessor),
                        typeof(AsyncLocalCurrentTenantAccessor)
                    );
                    services.AddTransient(
                        typeof(ICurrentTenant),
                        typeof(CurrentTenant)
                    );
                    services.OnRegistered(UnitOfWorkInterceptorRegistrar.RegisterIfNeeded);
                    // services.OnRegistred(UnitOfWorkInterceptorRegistrar.RegisterIfNeeded);
                    services.AddTransient(
                        typeof(IUnitOfWorkManager),
                        typeof(UnitOfWorkManager)
                    );
                    services.AddTransient(
                        typeof(IAmbientUnitOfWork),
                        typeof(AmbientUnitOfWork)
                    );
                    
                    services.AddSingleton<ICancellationTokenProvider>(NullCancellationTokenProvider.Instance);
                    services.AddSingleton<IClock, MockClock>();
                    //services.AddSingleton<IDistributedCache<AppleKeys>>();
                    services.AddHttpClient();

                    services.AddTransient(
                        typeof(IObjectMapper<>),
                        typeof(DefaultObjectMapper<>)
                    );
                    services.AddTransient(
                        typeof(IObjectMapper),
                        typeof(DefaultObjectMapper)
                    );
                    services.AddTransient(typeof(IAutoObjectMappingProvider),
                        typeof(AutoMapperAutoObjectMappingProvider));
                    services.AddTransient(sp => new MapperAccessor()
                    {
                        Mapper = sp.GetRequiredService<IMapper>()
                    });
                    services.AddTransient<IMapperAccessor>(provider => provider.GetRequiredService<MapperAccessor>());
                })
                .AddMemoryStreams(CAVerifierServerApplicationConsts.MessageStreamName)
                // .AddSimpleMessageStreamProvider(CAVerifierServerApplicationConsts.MessageStreamName)
                .AddMemoryGrainStorage("PubSubStore")
                .AddMemoryGrainStorageAsDefault();
        }
    }

    public class MapperAccessor : IMapperAccessor
    {
        public IMapper Mapper { get; set; }
    }

    public class DistributedCache<TCacheItem, TCacheKey> : IDistributedCache<TCacheItem, TCacheKey>
        where TCacheItem : class
    {
        public const string UowCacheName = "AbpDistributedCache";

        public ILogger<DistributedCache<TCacheItem, TCacheKey>> Logger { get; set; }

        protected string CacheName { get; set; }

        protected bool IgnoreMultiTenancy { get; set; }

        protected IDistributedCache Cache { get; }

        protected ICancellationTokenProvider CancellationTokenProvider { get; }

        protected SemaphoreSlim SyncSemaphore { get; }
        protected IUnitOfWorkManager UnitOfWorkManager { get; }
        private readonly AbpDistributedCacheOptions _distributedCacheOption;
        protected IDistributedCacheKeyNormalizer KeyNormalizer { get; }
        protected IDistributedCacheSerializer Serializer { get; }
        protected DistributedCacheEntryOptions DefaultCacheOptions;
        protected IServiceScopeFactory ServiceScopeFactory { get; }

        public DistributedCache(
            IOptions<AbpDistributedCacheOptions> distributedCacheOption,
            IDistributedCache cache,
            ICancellationTokenProvider cancellationTokenProvider,
            IDistributedCacheSerializer serializer,
            IDistributedCacheKeyNormalizer keyNormalizer,
            IServiceScopeFactory serviceScopeFactory,
            IUnitOfWorkManager unitOfWorkManager)
        {
            _distributedCacheOption = distributedCacheOption.Value;
            Cache = cache;
            CancellationTokenProvider = cancellationTokenProvider;
            Logger = NullLogger<DistributedCache<TCacheItem, TCacheKey>>.Instance;
            Serializer = serializer;
            KeyNormalizer = keyNormalizer;
            ServiceScopeFactory = serviceScopeFactory;
            UnitOfWorkManager = unitOfWorkManager;

            SyncSemaphore = new SemaphoreSlim(1, 1);

            SetDefaultOptions();
        }

        protected virtual void SetDefaultOptions()
        {
            CacheName = CacheNameAttribute.GetCacheName(typeof(TCacheItem));

            //IgnoreMultiTenancy
            IgnoreMultiTenancy = typeof(TCacheItem).IsDefined(typeof(IgnoreMultiTenancyAttribute), true);

            //Configure default cache entry options
            DefaultCacheOptions = GetDefaultCacheEntryOptions();
        }

        protected virtual DistributedCacheEntryOptions GetDefaultCacheEntryOptions()
        {
            foreach (var configure in _distributedCacheOption.CacheConfigurators)
            {
                var options = configure.Invoke(CacheName);
                if (options != null)
                {
                    return options;
                }
            }

            return _distributedCacheOption.GlobalCacheEntryOptions;
        }

        public TCacheItem Get(TCacheKey key, bool? hideErrors = null, bool considerUow = false)
        {
            throw new NotImplementedException();
        }

        public KeyValuePair<TCacheKey, TCacheItem>[] GetMany(IEnumerable<TCacheKey> keys, bool? hideErrors = null,
            bool considerUow = false)
        {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<TCacheKey, TCacheItem>[]> GetManyAsync(IEnumerable<TCacheKey> keys,
            bool? hideErrors = null, bool considerUow = false,
            CancellationToken token = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public virtual async Task<TCacheItem> GetAsync(
            TCacheKey key,
            bool? hideErrors = null,
            bool considerUow = false,
            CancellationToken token = default)
        {
            hideErrors = hideErrors ?? _distributedCacheOption.HideErrors;

            if (ShouldConsiderUow(considerUow))
            {
                var value = GetUnitOfWorkCache().GetOrDefault(key)?.GetUnRemovedValueOrNull();
                if (value != null)
                {
                    return value;
                }
            }

            byte[] cachedBytes;

            try
            {
                cachedBytes = await Cache.GetAsync(
                    NormalizeKey(key),
                    CancellationTokenProvider.FallbackToProvider(token)
                );
            }
            catch (System.Exception ex)
            {
                if (hideErrors == true)
                {
                    await HandleExceptionAsync(ex);
                    return null;
                }

                throw;
            }

            if (cachedBytes == null)
            {
                return null;
            }

            return Serializer.Deserialize<TCacheItem>(cachedBytes);
        }

        public TCacheItem GetOrAdd(TCacheKey key, Func<TCacheItem> factory,
            Func<DistributedCacheEntryOptions> optionsFactory = null, bool? hideErrors = null,
            bool considerUow = false)
        {
            throw new NotImplementedException();
        }

        public virtual async Task<TCacheItem> GetOrAddAsync(
            TCacheKey key,
            Func<Task<TCacheItem>> factory,
            Func<DistributedCacheEntryOptions> optionsFactory = null,
            bool? hideErrors = null,
            bool considerUow = false,
            CancellationToken token = default)
        {
            token = CancellationTokenProvider.FallbackToProvider(token);
            var value = await GetAsync(key, hideErrors, considerUow, token);
            if (value != null)
            {
                return value;
            }

            using (await SyncSemaphore.LockAsync(token))
            {
                value = await GetAsync(key, hideErrors, considerUow, token);
                if (value != null)
                {
                    return value;
                }

                value = await factory();

                if (ShouldConsiderUow(considerUow))
                {
                    var uowCache = GetUnitOfWorkCache();
                    if (uowCache.TryGetValue(key, out var item))
                    {
                        item.SetValue(value);
                    }
                    else
                    {
                        uowCache.Add(key, new UnitOfWorkCacheItem<TCacheItem>(value));
                    }
                }

                await SetAsync(key, value, optionsFactory?.Invoke(), hideErrors, considerUow, token);
            }

            return value;
        }

        protected virtual bool ShouldConsiderUow(bool considerUow)
        {
            return considerUow && UnitOfWorkManager.Current != null;
        }

        protected virtual Dictionary<TCacheKey, UnitOfWorkCacheItem<TCacheItem>> GetUnitOfWorkCache()
        {
            if (UnitOfWorkManager.Current == null)
            {
                throw new AbpException($"There is no active UOW.");
            }

            return UnitOfWorkManager.Current.GetOrAddItem(GetUnitOfWorkCacheKey(),
                key => new Dictionary<TCacheKey, UnitOfWorkCacheItem<TCacheItem>>());
        }

        protected virtual string GetUnitOfWorkCacheKey()
        {
            return UowCacheName + CacheName;
        }

        public KeyValuePair<TCacheKey, TCacheItem>[] GetOrAddMany(IEnumerable<TCacheKey> keys,
            Func<IEnumerable<TCacheKey>, List<KeyValuePair<TCacheKey, TCacheItem>>> factory,
            Func<DistributedCacheEntryOptions> optionsFactory = null, bool? hideErrors = null,
            bool considerUow = false)
        {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<TCacheKey, TCacheItem>[]> GetOrAddManyAsync(IEnumerable<TCacheKey> keys,
            Func<IEnumerable<TCacheKey>, Task<List<KeyValuePair<TCacheKey, TCacheItem>>>> factory,
            Func<DistributedCacheEntryOptions> optionsFactory = null, bool? hideErrors = null,
            bool considerUow = false, CancellationToken token = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public void Set(TCacheKey key, TCacheItem value, DistributedCacheEntryOptions options = null,
            bool? hideErrors = null,
            bool considerUow = false)
        {
            throw new NotImplementedException();
        }

        public virtual async Task SetAsync(
            TCacheKey key,
            TCacheItem value,
            DistributedCacheEntryOptions options = null,
            bool? hideErrors = null,
            bool considerUow = false,
            CancellationToken token = default)
        {
            async Task SetRealCache()
            {
                hideErrors = hideErrors ?? _distributedCacheOption.HideErrors;

                try
                {
                    await Cache.SetAsync(
                        NormalizeKey(key),
                        Serializer.Serialize(value),
                        options ?? DefaultCacheOptions,
                        CancellationTokenProvider.FallbackToProvider(token)
                    );
                }
                catch (System.Exception ex)
                {
                    if (hideErrors == true)
                    {
                        await HandleExceptionAsync(ex);
                        return;
                    }

                    throw;
                }
            }

            if (ShouldConsiderUow(considerUow))
            {
                var uowCache = GetUnitOfWorkCache();
                if (uowCache.TryGetValue(key, out _))
                {
                    uowCache[key].SetValue(value);
                }
                else
                {
                    uowCache.Add(key, new UnitOfWorkCacheItem<TCacheItem>(value));
                }

                // ReSharper disable once PossibleNullReferenceException
                UnitOfWorkManager.Current.OnCompleted(SetRealCache);
            }
            else
            {
                await SetRealCache();
            }
        }

        protected virtual string NormalizeKey(TCacheKey key)
        {
            return KeyNormalizer.NormalizeKey(
                new DistributedCacheKeyNormalizeArgs(
                    key.ToString(),
                    CacheName,
                    IgnoreMultiTenancy
                )
            );
        }

        protected virtual async Task HandleExceptionAsync(System.Exception ex)
        {
            Logger.LogException(ex, LogLevel.Warning);

            using (var scope = ServiceScopeFactory.CreateScope())
            {
                await scope.ServiceProvider
                    .GetRequiredService<IExceptionNotifier>()
                    .NotifyAsync(new ExceptionNotificationContext(ex, LogLevel.Warning));
            }
        }

        public void SetMany(IEnumerable<KeyValuePair<TCacheKey, TCacheItem>> items,
            DistributedCacheEntryOptions options = null, bool? hideErrors = null,
            bool considerUow = false)
        {
            throw new NotImplementedException();
        }

        public Task SetManyAsync(IEnumerable<KeyValuePair<TCacheKey, TCacheItem>> items,
            DistributedCacheEntryOptions options = null, bool? hideErrors = null,
            bool considerUow = false, CancellationToken token = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public void Refresh(TCacheKey key, bool? hideErrors = null)
        {
            throw new NotImplementedException();
        }

        public Task RefreshAsync(TCacheKey key, bool? hideErrors = null,
            CancellationToken token = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public void RefreshMany(IEnumerable<TCacheKey> keys, bool? hideErrors = null)
        {
            throw new NotImplementedException();
        }

        public Task RefreshManyAsync(IEnumerable<TCacheKey> keys, bool? hideErrors = null,
            CancellationToken token = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public void Remove(TCacheKey key, bool? hideErrors = null, bool considerUow = false)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAsync(TCacheKey key, bool? hideErrors = null, bool considerUow = false,
            CancellationToken token = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public void RemoveMany(IEnumerable<TCacheKey> keys, bool? hideErrors = null, bool considerUow = false)
        {
            throw new NotImplementedException();
        }

        public Task RemoveManyAsync(IEnumerable<TCacheKey> keys, bool? hideErrors = null, bool considerUow = false,
            CancellationToken token = new CancellationToken())
        {
            throw new NotImplementedException();
        }
    }
    
    public class AsyncLocalCurrentTenantAccessor : ICurrentTenantAccessor,ISingletonDependency
    {
        public static AsyncLocalCurrentTenantAccessor Instance { get; } = new();

        public BasicTenantInfo Current {
            get => _currentScope.Value;
            set => _currentScope.Value = value;
        }

        private readonly AsyncLocal<BasicTenantInfo> _currentScope;

        public AsyncLocalCurrentTenantAccessor()
        {
            _currentScope = new AsyncLocal<BasicTenantInfo>();
        }
    }

    private class TestClientBuilderConfigurator : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder) => clientBuilder
            .AddMemoryStreams(CAVerifierServerApplicationConsts.MessageStreamName);
            // .AddSimpleMessageStreamProvider(CAVerifierServerApplicationConsts.MessageStreamName);
    }
}