/*
 * Hypernex.Unity is Licensed under GPL 3.0
 * You can view the license here: https://github.com/TigersUniverse/Hypernex.Unity/blob/main/LICENSE
 * Changes:
 *   + Use MelonLogger for Logging
 *   + Rewrite Config for MelonPreferences
 *   + Use ChilloutVR Identities
 *   + Use Runner
 */

using System.Reflection;
using ABI_RC.API;
using MelonLoader;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VRCFaceTracking.Core.Contracts.Services;
using VRCFT2CVR;

namespace Hypernex.ExtendedTracking
{
    public static class FaceTrackingServices
    {
        public class FTLogger : ILogger
        {
            private string p;
            private MelonLogger.Instance logger;

            public FTLogger(string c, MelonLogger.Instance logger)
            {
                p = $"[{c}] ";
                this.logger = logger;
            }
            
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                Runner.InvokeActionOnMainThread(new Action(() =>
                {
                    switch (logLevel)
                    {
                        case LogLevel.Information:
                            logger.Msg(p + state);
                            break;
                        case LogLevel.Warning:
                            logger.Warning(p + state);
                            break;
                        case LogLevel.Error:
                            logger.Error(p + state);
                            break;
                        case LogLevel.Critical:
                            logger.Error(new Exception(p, exception));
                            break;
                        default:
                            logger.Msg(p + state);
                            break;
                    }
                }));
            }

            public bool IsEnabled(LogLevel logLevel) => true;

            public IDisposable BeginScope<TState>(TState state) where TState : notnull => new _();
        }
        
        private class _ : IDisposable{public void Dispose(){}}
        
        public class FTLoggerFactory: ILoggerFactory
        {
            private MelonLogger.Instance logger;

            public FTLoggerFactory(MelonLogger.Instance logger) => this.logger = logger;
            
            public void Dispose(){}

            public ILogger CreateLogger(string categoryName) => new FTLogger(categoryName, logger);

            public void AddProvider(ILoggerProvider provider){}
        }

        public class FTDispatcher : IDispatcherService
        {
            public void Run(Action action) => Runner.InvokeActionOnMainThread(action);
        }

        public class FTSettings : ILocalSettingsService
        {
            public Task<T> ReadSettingAsync<T>(string key)
            {
                Dictionary<string, string> v = Config.facialTrackingSettings.Value;
                if (!v.ContainsKey(key))
                    return Task.FromResult((T) default);
                return Task.FromResult(
                    JsonConvert.DeserializeObject<T>(v[key])!);
            }

            public Task SaveSettingAsync<T>(string key, T value)
            {
                return Task.Run(() =>
                {
                    Dictionary<string, string> v = Config.facialTrackingSettings.Value;
                    if (v.ContainsKey(key))
                        v[key] = JsonConvert.SerializeObject(value);
                    else
                        v.Add(key, JsonConvert.SerializeObject(value));
                    Config.facialTrackingSettings.Value = v;
                });
            }

            public Task<T> ReadSettingAsync<T>(string key, T defaultValue = default(T), bool forceLocal = false)
            {
                Dictionary<string, string> v = Config.facialTrackingSettings.Value;
                if (!v.ContainsKey(key))
                    return Task.FromResult(defaultValue);
                return Task.FromResult(JsonConvert.DeserializeObject<T>(v[key])!);
            }

            public Task SaveSettingAsync<T>(string key, T value, bool forceLocal = false) =>
                SaveSettingAsync(key, value);

            // Why do I have to do this? Why not just Serialize the object??
            private Dictionary<MemberInfo, SavedSettingAttribute> GetSavedSettings(object target)
            {
                Dictionary<MemberInfo, SavedSettingAttribute> members = new();
                Type targetType = target.GetType();
                foreach (FieldInfo fieldInfo in targetType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic |
                                                                     BindingFlags.Public))
                {
                    SavedSettingAttribute[] attributes = fieldInfo.GetCustomAttributes(typeof(SavedSettingAttribute))
                        .Select(x => (SavedSettingAttribute) x).ToArray();
                    if(attributes.Length <= 0) continue;
                    members.Add(fieldInfo, attributes[0]);
                }
                foreach (PropertyInfo propertyInfo in targetType.GetProperties(BindingFlags.Instance |
                                                                               BindingFlags.NonPublic |
                                                                               BindingFlags.Public))
                {
                    SavedSettingAttribute[] attributes = propertyInfo.GetCustomAttributes(typeof(SavedSettingAttribute))
                        .Select(x => (SavedSettingAttribute) x).ToArray();
                    if(attributes.Length <= 0) continue;
                    members.Add(propertyInfo, attributes[0]);
                }
                return members;
            }

            public Task Save(object target)
            {
                Dictionary<string, object> values = new();
                foreach (KeyValuePair<MemberInfo,SavedSettingAttribute> savedSetting in GetSavedSettings(target))
                {
                    object value;
                    value = savedSetting.Key is FieldInfo
                        ? ((FieldInfo) savedSetting.Key).GetValue(target)
                        : ((PropertyInfo) savedSetting.Key).GetValue(target);
                    value ??= savedSetting.Value.Default();
                    if(value == null) continue;
                    values.Add(savedSetting.Value.GetName(), value);
                }
                return SaveSettingAsync(target.GetType().FullName!.Replace(".", ""), values);
            }

            public Task Load(object target)
            {
                Dictionary<string, object> values =
                    ReadSettingAsync<Dictionary<string, object>>(target.GetType().FullName!.Replace(".", "")).Result;
                if (values == null) return Task.CompletedTask;
                foreach (KeyValuePair<MemberInfo,SavedSettingAttribute> savedSetting in GetSavedSettings(target))
                {
                    if (savedSetting.Key is FieldInfo)
                    {
                        FieldInfo fieldInfo = (FieldInfo) savedSetting.Key;
                        object value;
                        if (!values.TryGetValue(savedSetting.Value.GetName(), out value))
                            value = savedSetting.Value.Default();
                        fieldInfo.SetValue(target, Convert.ChangeType(value, fieldInfo.FieldType));
                    }
                    else if (savedSetting.Key is PropertyInfo)
                    {
                        PropertyInfo propertyInfo = (PropertyInfo) savedSetting.Key;
                        object value;
                        if (!values.TryGetValue(savedSetting.Value.GetName(), out value))
                            value = savedSetting.Value.Default();
                        propertyInfo.SetValue(target, Convert.ChangeType(value, propertyInfo.PropertyType));
                    }
                }
                return Task.CompletedTask;
            }
        }

        public class ChilloutVRIdentity : IIdentityService
        {
            private Player player;

            public ChilloutVRIdentity(Player player) => this.player = player;

            public string GetUniqueUserId() => player.UserID;
        }
    }
}