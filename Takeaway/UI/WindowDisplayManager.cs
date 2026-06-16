#pragma warning disable 1998
#pragma warning disable 4014
#pragma warning disable 0649
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Linq;

namespace Fale7_POS
{
    public partial class MainWindow : Window
    {
        private static readonly bool AlwaysFullscreenMode = true;
        private static readonly bool PixelPerfectMode = true;
        private const double PixelPerfectDesignWidth = 1280d;
        private const double PixelPerfectDesignHeight = 1024d;
        private static readonly bool AllowOfflineCachedLogin = true;
        private static readonly TimeSpan LocalCashierBackendAliasMaxAge = TimeSpan.FromDays(7);

        private readonly CultureInfo _arEg = new CultureInfo("ar-EG");
        private readonly Random _random = new Random();
        private readonly List<MenuCategory> _menu = new List<MenuCategory>();
        private readonly DispatcherTimer _clockTimer;

        private SessionRecord _currentSession;
        private TakeawayStateRecord _takeawayState;
        private string _activeCategory;
        private string _selectedRowKey;
        private bool _isAdminMode;
        private bool _loginInProgress;
        private readonly object _localCashierCredentialsSync = new object();
        private LocalCashierCredentialsCacheRecord _localCashierCredentials;
        private const string VirtualPrinterActivationCode = "662010662010";
        private readonly StringBuilder _virtualPrinterActivationBuffer = new StringBuilder();
        private bool _virtualPrinterMode;

        public MainWindow()
        {
            InitializeComponent();
            PreviewMouseDown += Window_PreviewMouseDown;
            ConfigureWindowSizingMode();
            RememberMeCheckBox.IsChecked = false;
            CleanupLegacyLocalFilesOnStartup();
            EnsureSupabaseBackendExportShell();
            InitializeTakeawayMenu();
            LoadMenuStateFromDisk();
            LoadTakeawayStateFromDisk();
            NormalizeTakeawayState();
            LoadLocalCashierCredentialsCache();
            LoadLocalToBackendIdMap();
            InitializePosOfflineSensing();
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += ClockTimer_Tick;
            _clockTimer.Start();
            UpdateTakeawayHeaderClock();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (AlwaysFullscreenMode)
            {
                ApplyFullscreenMode();
            }
            else if (PixelPerfectMode)
            {
                AdjustWindowToPixelPerfectClientArea();
            }

            ShowLoginView();

            BeginSupabaseBootstrapIfConfigured();
        }

        private void ConfigureWindowSizingMode()
        {
            if (AlwaysFullscreenMode)
            {
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Normal;
                ResizeMode = ResizeMode.NoResize;
                MinWidth = 0;
                MinHeight = 0;
                MaxWidth = double.PositiveInfinity;
                MaxHeight = double.PositiveInfinity;
                return;
            }

            if (PixelPerfectMode)
            {
                WindowState = WindowState.Normal;
                ResizeMode = ResizeMode.NoResize;
                Width = PixelPerfectDesignWidth;
                Height = PixelPerfectDesignHeight;
                MinWidth = 0;
                MinHeight = 0;
                MaxWidth = double.PositiveInfinity;
                MaxHeight = double.PositiveInfinity;
                return;
            }

            ResizeMode = ResizeMode.CanResize;
            MinWidth = 1000;
            MinHeight = 700;
            MaxWidth = double.PositiveInfinity;
            MaxHeight = double.PositiveInfinity;
            Width = PixelPerfectDesignWidth;
            Height = PixelPerfectDesignHeight;
        }

        private void ApplyFullscreenMode()
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            MinWidth = 0;
            MinHeight = 0;
            MaxWidth = double.PositiveInfinity;
            MaxHeight = double.PositiveInfinity;
            WindowState = WindowState.Normal;
            WindowState = WindowState.Maximized;
        }

        private void AdjustWindowToPixelPerfectClientArea()
        {
            if (RootViewbox == null) return;

            for (int i = 0; i < 3; i++)
            {
                UpdateLayout();
                var dx = PixelPerfectDesignWidth - RootViewbox.ActualWidth;
                var dy = PixelPerfectDesignHeight - RootViewbox.ActualHeight;
                if (Math.Abs(dx) < 0.5 && Math.Abs(dy) < 0.5)
                {
                    break;
                }

                Width += dx;
                Height += dy;
            }

            MinWidth = Width;
            MaxWidth = Width;
            MinHeight = Height;
            MaxHeight = Height;
        }

        private void ClockTimer_Tick(object sender, EventArgs e)
        {
            UpdateTakeawayHeaderClock();
            if (DeliveryView != null && DeliveryView.Visibility == Visibility.Visible)
            {
                RenderDeliveryQueue();
            }
        }

        private void UpdateTakeawayHeaderClock()
        {
            var now = DateTime.Now;
            TakeawayHeaderDateRun.Text = now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            TakeawayHeaderClockRun.Text = now.ToString("hh:mm:ss tt", _arEg);
            if (DeliveryHeaderDateRun != null) DeliveryHeaderDateRun.Text = now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            if (DeliveryHeaderClockRun != null) DeliveryHeaderClockRun.Text = now.ToString("hh:mm:ss tt", _arEg);
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            PerformLoginAsync();
        }

        private void LoginPasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                PerformLoginAsync();
            }
        }

        private async void PerformLoginAsync()
        {
            if (_loginInProgress) return;
            _loginInProgress = true;
            try
            {
                LoginErrorText.Text = string.Empty;
                var user = (LoginUsernameTextBox.Text ?? string.Empty).Trim();
                var pass = LoginPasswordBox.Password ?? string.Empty;

                if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
                {
                    LoginErrorText.Text = "Username and password / PIN are required.";
                    return;
                }

                if (LoginButton != null) LoginButton.IsEnabled = false;
                Mouse.OverrideCursor = Cursors.Wait;
                try
                {
                    var loginResult = await Task.Run(delegate
                    {
                        string backendRole;
                        string backendUsername;
                        string backendUserId;
                        string backendRawRole;
                        string backendSessionToken;
                        long backendSessionExpiresAtMillis;
                        string backendError;
                        var backendAttempt = TryPerformBackendLogin(
                            user,
                            pass,
                            out backendRole,
                            out backendUsername,
                            out backendUserId,
                            out backendRawRole,
                            out backendSessionToken,
                            out backendSessionExpiresAtMillis,
                            out backendError);
                        if (backendAttempt == 1)
                        {
                            return new BackendLoginAttemptResult
                            {
                                Attempt = 1,
                                UsedLocalCache = false,
                                Role = backendRole,
                                Username = backendUsername,
                                UserId = backendUserId,
                                BackendRole = backendRawRole,
                                SessionToken = backendSessionToken,
                                SessionExpiresAtMillis = backendSessionExpiresAtMillis,
                                Error = null
                            };
                        }

                        // Allow local-cache login when the backend is unreachable
                        // or when the backend staff secret is missing/blocked.
                        var allowOfflineCacheFallback = AllowOfflineCachedLogin
                            && (
                                backendAttempt == 0
                                || string.Equals(backendError, "backend_unreachable", StringComparison.OrdinalIgnoreCase)
                                || IsMissingSecretBackendError(backendError)
                            );
                        if (!allowOfflineCacheFallback)
                        {
                            return new BackendLoginAttemptResult
                            {
                                Attempt = -1,
                                Role = null,
                                Username = null,
                                UserId = null,
                                BackendRole = null,
                                SessionToken = null,
                                Error = !string.IsNullOrWhiteSpace(backendError)
                                    ? backendError
                                    : "Backend login failed."
                            };
                        }

                        string localRole;
                        string localUsername;
                        string localUserId;
                        string localBackendRole;
                        string localError;
                        var localAttempt = TryPerformLocalCachedLogin(
                            user,
                            pass,
                            out localRole,
                            out localUsername,
                            out localUserId,
                            out localBackendRole,
                            out localError);
                        if (localAttempt == 1)
                        {
                            return new BackendLoginAttemptResult
                            {
                                Attempt = 1,
                                UsedLocalCache = true,
                                Role = localRole,
                                Username = localUsername,
                                UserId = localUserId,
                                BackendRole = localBackendRole,
                                SessionToken = null,
                                SessionExpiresAtMillis = 0,
                                Error = null
                            };
                        }

                        return new BackendLoginAttemptResult
                        {
                            Attempt = -1,
                            Role = null,
                            Username = null,
                            UserId = null,
                            BackendRole = null,
                            SessionToken = null,
                            SessionExpiresAtMillis = 0,
                            Error = !string.IsNullOrWhiteSpace(localError) ? localError : backendError
                        };
                    });

                    if (loginResult.Attempt == 1)
                    {
                        if (loginResult.UsedLocalCache)
                        {
                            RememberLocalCashierCredentialForDeferredSync(loginResult.UserId, pass);
                        }
                        _currentSession = new SessionRecord
                        {
                            Username = loginResult.Username ?? user,
                            Role = loginResult.Role,
                            UserId = loginResult.UserId,
                            BackendRole = loginResult.BackendRole,
                            SessionToken = loginResult.SessionToken,
                            SessionExpiresAtMillis = loginResult.SessionExpiresAtMillis
                        };
                        if (RememberMeCheckBox.IsChecked == true) SaveSessionToDisk(_currentSession); else DeleteSessionFile();
                        QueuePosOfflineSync();
                        if (!NavigateToRole(_currentSession.Role, _currentSession.Username))
                        {
                            LoginErrorText.Text = "Unsupported role: " + (_currentSession.Role ?? string.Empty);
                            return;
                        }
                        return;
                    }

                    LoginErrorText.Text = string.IsNullOrWhiteSpace(loginResult.Error)
                        ? "Invalid credentials."
                        : loginResult.Error;
                    return;
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                    if (LoginButton != null) LoginButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                AppendPosRuntimeLog("PerformLoginAsync", ex);
                LoginErrorText.Text = "Login failed: " + ex.Message;
            }
            finally
            {
                _loginInProgress = false;
            }
        }

        private static bool IsInternetAvailableForLogin()
        {
            try
            {
                return System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
            }
            catch
            {
                return false;
            }
        }

        private void LoadLocalCashierCredentialsCache()
        {
            lock (_localCashierCredentialsSync)
            {
                _localCashierCredentials = LoadCashierCredentialsCacheFromDisk() ?? new LocalCashierCredentialsCacheRecord();
                var changed = NormalizeLocalCashierCredentialsCacheNoLock();
                if (changed) SaveCashierCredentialsCacheToDisk(_localCashierCredentials);
            }
        }

        private bool NormalizeLocalCashierCredentialsCacheNoLock()
        {
            var changed = false;
            if (_localCashierCredentials == null) _localCashierCredentials = new LocalCashierCredentialsCacheRecord();
            if (_localCashierCredentials.Version <= 0) _localCashierCredentials.Version = 1;
            if (_localCashierCredentials.Entries == null) _localCashierCredentials.Entries = new List<LocalCashierCredentialRecord>();

            for (int i = _localCashierCredentials.Entries.Count - 1; i >= 0; i--)
            {
                var row = _localCashierCredentials.Entries[i];
                if (row == null)
                {
                    _localCashierCredentials.Entries.RemoveAt(i);
                    changed = true;
                    continue;
                }

                row.UserId = (row.UserId ?? string.Empty).Trim();
                row.Username = (row.Username ?? string.Empty).Trim();
                row.DisplayName = (row.DisplayName ?? string.Empty).Trim();
                row.Phone = (row.Phone ?? string.Empty).Trim();
                row.Email = (row.Email ?? string.Empty).Trim();
                row.Role = (row.Role ?? string.Empty).Trim().ToUpperInvariant();
                row.PasswordHash = (row.PasswordHash ?? string.Empty).Trim();
                row.PasswordSecretProtected = (row.PasswordSecretProtected ?? string.Empty).Trim();
                row.UserUpdatedAt = (row.UserUpdatedAt ?? string.Empty).Trim();
                row.SecretUpdatedAt = (row.SecretUpdatedAt ?? string.Empty).Trim();
                row.LastSyncedAt = (row.LastSyncedAt ?? string.Empty).Trim();

                if (!string.IsNullOrWhiteSpace(row.PasswordSecretProtected) && !string.IsNullOrWhiteSpace(row.PasswordHash))
                {
                    // DPAPI-protected local secrets supersede the old MD5 verifier.
                    row.PasswordHash = string.Empty;
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(row.UserId))
                {
                    _localCashierCredentials.Entries.RemoveAt(i);
                    changed = true;
                    continue;
                }
            }

            var mergedByUserId = new Dictionary<string, LocalCashierCredentialRecord>(StringComparer.OrdinalIgnoreCase);
            var orderedUserIds = new List<string>();
            for (var i = 0; i < _localCashierCredentials.Entries.Count; i++)
            {
                var row = _localCashierCredentials.Entries[i];
                if (row == null || string.IsNullOrWhiteSpace(row.UserId)) continue;

                if (!mergedByUserId.TryGetValue(row.UserId, out var existing))
                {
                    mergedByUserId[row.UserId] = row;
                    orderedUserIds.Add(row.UserId);
                    continue;
                }

                mergedByUserId[row.UserId] = MergeDuplicateLocalCashierCredential(existing, row);
            }

            var normalizedEntries = new List<LocalCashierCredentialRecord>(orderedUserIds.Count);
            for (var i = 0; i < orderedUserIds.Count; i++)
            {
                var userId = orderedUserIds[i];
                if (!mergedByUserId.TryGetValue(userId, out var row) || row == null) continue;
                normalizedEntries.Add(row);
            }
            if (_localCashierCredentials.Entries.Count != normalizedEntries.Count)
            {
                changed = true;
            }
            _localCashierCredentials.Entries = normalizedEntries;
            return changed;
        }

        private bool HasLocalCashierCredentialsCached()
        {
            lock (_localCashierCredentialsSync)
            {
                NormalizeLocalCashierCredentialsCacheNoLock();
                return _localCashierCredentials.Entries.Count > 0;
            }
        }

        private static bool IsCachedStaffRole(string backendRole)
        {
            var role = (backendRole ?? string.Empty).Trim().ToUpperInvariant();
            return role == "CASHIER" || role == "ADMIN_POS" || role == "ADMIN" || role == "DRIVER";
        }

        private static string MapBackendRoleToLocalRole(string backendRole)
        {
            var role = (backendRole ?? string.Empty).Trim().ToUpperInvariant();
            if (role == "CASHIER") return "TAKEAWAY";
            if (role == "DRIVER") return "DELIVERY";
            if (role == "ADMIN_POS" || role == "ADMINPOS" || role == "ADMIN") return "ADMIN";
            return role;
        }

        private static int GetStaffRoleLoginPriority(string backendRole)
        {
            var role = (backendRole ?? string.Empty).Trim().ToUpperInvariant();
            if (role == "ADMIN_POS") return 1;
            if (role == "ADMIN") return 2;
            if (role == "CASHIER") return 3;
            if (role == "DRIVER") return 4;
            return 10;
        }

        private static bool EqualsTrimmedIgnoreCase(string left, string right)
        {
            return string.Equals(
                (left ?? string.Empty).Trim(),
                (right ?? string.Empty).Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        private static LocalCashierCredentialRecord MergeDuplicateLocalCashierCredential(
            LocalCashierCredentialRecord left,
            LocalCashierCredentialRecord right)
        {
            if (left == null) return right;
            if (right == null) return left;

            var leftFreshness = GetLocalCashierCredentialFreshnessUtc(left);
            var rightFreshness = GetLocalCashierCredentialFreshnessUtc(right);
            var primary = rightFreshness > leftFreshness ? right : left;
            var secondary = ReferenceEquals(primary, left) ? right : left;

            var merged = CloneLocalCashierCredential(primary);
            FillLocalCashierCredentialBlanks(merged, secondary);
            if (string.IsNullOrWhiteSpace(merged.PasswordHash) && !string.IsNullOrWhiteSpace(secondary.PasswordHash))
            {
                merged.PasswordHash = secondary.PasswordHash.Trim();
            }
            if (string.IsNullOrWhiteSpace(merged.PasswordSecretProtected) && !string.IsNullOrWhiteSpace(secondary.PasswordSecretProtected))
            {
                merged.PasswordSecretProtected = secondary.PasswordSecretProtected.Trim();
            }

            merged.Dirty = primary.Dirty || secondary.Dirty;
            return merged;
        }

        private static LocalCashierCredentialRecord CloneLocalCashierCredential(LocalCashierCredentialRecord source)
        {
            if (source == null) return null;
            return new LocalCashierCredentialRecord
            {
                UserId = (source.UserId ?? string.Empty).Trim(),
                Username = (source.Username ?? string.Empty).Trim(),
                DisplayName = (source.DisplayName ?? string.Empty).Trim(),
                Phone = (source.Phone ?? string.Empty).Trim(),
                Email = (source.Email ?? string.Empty).Trim(),
                Role = (source.Role ?? string.Empty).Trim().ToUpperInvariant(),
                IsActive = source.IsActive,
                PasswordHash = (source.PasswordHash ?? string.Empty).Trim(),
                PasswordSecretProtected = (source.PasswordSecretProtected ?? string.Empty).Trim(),
                UserUpdatedAt = (source.UserUpdatedAt ?? string.Empty).Trim(),
                SecretUpdatedAt = (source.SecretUpdatedAt ?? string.Empty).Trim(),
                Dirty = source.Dirty,
                LastSyncedAt = (source.LastSyncedAt ?? string.Empty).Trim()
            };
        }

        private static void FillLocalCashierCredentialBlanks(LocalCashierCredentialRecord target, LocalCashierCredentialRecord source)
        {
            if (target == null || source == null) return;

            if (string.IsNullOrWhiteSpace(target.Username)) target.Username = (source.Username ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(target.DisplayName)) target.DisplayName = (source.DisplayName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(target.Phone)) target.Phone = (source.Phone ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(target.Email)) target.Email = (source.Email ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(target.Role)) target.Role = (source.Role ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(target.PasswordSecretProtected)) target.PasswordSecretProtected = (source.PasswordSecretProtected ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(target.UserUpdatedAt)) target.UserUpdatedAt = (source.UserUpdatedAt ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(target.SecretUpdatedAt)) target.SecretUpdatedAt = (source.SecretUpdatedAt ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(target.LastSyncedAt)) target.LastSyncedAt = (source.LastSyncedAt ?? string.Empty).Trim();
            if (!target.IsActive && source.IsActive) target.IsActive = true;
        }

        private static DateTime GetLocalCashierCredentialFreshnessUtc(LocalCashierCredentialRecord row)
        {
            if (row == null) return DateTime.MinValue;

            var freshest = DateTime.MinValue;
            freshest = MaxUtcDateTime(freshest, TryParseUtcDateTime(row.UserUpdatedAt));
            freshest = MaxUtcDateTime(freshest, TryParseUtcDateTime(row.SecretUpdatedAt));
            freshest = MaxUtcDateTime(freshest, TryParseUtcDateTime(row.LastSyncedAt));
            return freshest;
        }

        private static bool IsFreshLocalCashierCredentialForBackendAlias(LocalCashierCredentialRecord row)
        {
            if (row == null || !row.IsActive || !IsCachedStaffRole(row.Role)) return false;

            var freshest = GetLocalCashierCredentialFreshnessUtc(row);
            if (freshest == DateTime.MinValue) return false;

            return freshest >= DateTime.UtcNow.Subtract(LocalCashierBackendAliasMaxAge);
        }

        private static DateTime TryParseUtcDateTime(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return DateTime.MinValue;
            if (DateTime.TryParse(
                raw.Trim(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
            {
                return parsed.ToUniversalTime();
            }
            return DateTime.MinValue;
        }

        private static DateTime MaxUtcDateTime(DateTime left, DateTime right)
        {
            return left >= right ? left : right;
        }

        private static string ProtectLocalSecret(string raw)
        {
            var secret = raw ?? string.Empty;
            if (string.IsNullOrWhiteSpace(secret)) return string.Empty;

            try
            {
                var bytes = Encoding.UTF8.GetBytes(secret);
                var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(protectedBytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string UnprotectLocalSecret(string protectedValue)
        {
            var cipher = (protectedValue ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(cipher)) return string.Empty;

            try
            {
                var protectedBytes = Convert.FromBase64String(cipher);
                var bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool VerifyProtectedLocalSecret(string protectedValue, string rawCandidate)
        {
            var plain = UnprotectLocalSecret(protectedValue);
            if (string.IsNullOrWhiteSpace(plain)) return false;
            return string.Equals(plain, rawCandidate ?? string.Empty, StringComparison.Ordinal);
        }

        private LocalCashierCredentialRecord FindLocalCashierCredentialByUserIdNoLock(string userId)
        {
            var id = (userId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(id) || _localCashierCredentials == null || _localCashierCredentials.Entries == null) return null;

            for (var i = 0; i < _localCashierCredentials.Entries.Count; i++)
            {
                var row = _localCashierCredentials.Entries[i];
                if (row == null) continue;
                if (EqualsTrimmedIgnoreCase(row.UserId, id)) return row;
            }
            return null;
        }

        private static bool LocalIdentifierMatches(LocalCashierCredentialRecord row, string identifier)
        {
            if (row == null || string.IsNullOrWhiteSpace(identifier)) return false;
            var ident = identifier.Trim();
            return EqualsTrimmedIgnoreCase(row.Username, ident)
                   || EqualsTrimmedIgnoreCase(row.DisplayName, ident)
                   || EqualsTrimmedIgnoreCase(row.Phone, ident)
                   || EqualsTrimmedIgnoreCase(row.Email, ident)
                   || EqualsTrimmedIgnoreCase(row.UserId, ident);
        }

        private List<LocalCashierCredentialRecord> GetLocalCashierCredentialCandidatesSnapshot(string identifier)
        {
            var ident = (identifier ?? string.Empty).Trim();
            var orderedUserIds = new List<string>();
            var mergedByUserId = new Dictionary<string, LocalCashierCredentialRecord>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(ident)) return new List<LocalCashierCredentialRecord>();

            lock (_localCashierCredentialsSync)
            {
                NormalizeLocalCashierCredentialsCacheNoLock();
                for (var i = 0; i < _localCashierCredentials.Entries.Count; i++)
                {
                    var row = _localCashierCredentials.Entries[i];
                    if (row == null) continue;
                    if (!row.IsActive) continue;
                    if (!IsCachedStaffRole(row.Role)) continue;
                    if (!LocalIdentifierMatches(row, ident)) continue;

                    var clone = CloneLocalCashierCredential(row);
                    var userId = (clone == null ? string.Empty : (clone.UserId ?? string.Empty).Trim());
                    if (string.IsNullOrWhiteSpace(userId)) continue;

                    if (mergedByUserId.TryGetValue(userId, out var existing))
                    {
                        mergedByUserId[userId] = MergeDuplicateLocalCashierCredential(existing, clone);
                        continue;
                    }

                    mergedByUserId[userId] = clone;
                    orderedUserIds.Add(userId);
                }
            }

            var list = new List<LocalCashierCredentialRecord>(orderedUserIds.Count);
            for (var i = 0; i < orderedUserIds.Count; i++)
            {
                var userId = orderedUserIds[i];
                if (!mergedByUserId.TryGetValue(userId, out var row) || row == null) continue;
                list.Add(row);
            }

            list.Sort(delegate (LocalCashierCredentialRecord left, LocalCashierCredentialRecord right)
            {
                var leftPriority = GetStaffRoleLoginPriority(left == null ? null : left.Role);
                var rightPriority = GetStaffRoleLoginPriority(right == null ? null : right.Role);
                var priorityCompare = leftPriority.CompareTo(rightPriority);
                if (priorityCompare != 0) return priorityCompare;

                var leftFreshness = GetLocalCashierCredentialFreshnessUtc(left);
                var rightFreshness = GetLocalCashierCredentialFreshnessUtc(right);
                var freshnessCompare = rightFreshness.CompareTo(leftFreshness);
                if (freshnessCompare != 0) return freshnessCompare;

                var leftUserId = left == null ? string.Empty : (left.UserId ?? string.Empty).Trim();
                var rightUserId = right == null ? string.Empty : (right.UserId ?? string.Empty).Trim();
                return string.Compare(leftUserId, rightUserId, StringComparison.OrdinalIgnoreCase);
            });

            return list;
        }

        private static void AddUniqueBackendLoginIdentifier(List<string> identifiers, string candidate)
        {
            if (identifiers == null) return;

            var clean = (candidate ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(clean)) return;

            for (var i = 0; i < identifiers.Count; i++)
            {
                if (string.Equals(identifiers[i], clean, StringComparison.OrdinalIgnoreCase)) return;
            }

            identifiers.Add(clean);
        }

        private List<string> BuildBackendLoginIdentifierCandidates(string identifier)
        {
            var identifiers = new List<string>();
            AddUniqueBackendLoginIdentifier(identifiers, identifier);

            var localMatches = GetLocalCashierCredentialCandidatesSnapshot(identifier);

            for (var i = 0; i < localMatches.Count; i++)
            {
                var row = localMatches[i];
                if (row == null) continue;
                if (!IsFreshLocalCashierCredentialForBackendAlias(row)) continue;

                // Only trust recent cache snapshots for backend alias expansion.
                AddUniqueBackendLoginIdentifier(identifiers, row.Username);
                AddUniqueBackendLoginIdentifier(identifiers, row.Phone);
                AddUniqueBackendLoginIdentifier(identifiers, row.Email);
                AddUniqueBackendLoginIdentifier(identifiers, row.UserId);
            }
            return identifiers;
        }

        private static string ComputeMd5Hex(string raw)
        {
            var text = raw ?? string.Empty;
            using (var md5 = MD5.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(text);
                var hash = md5.ComputeHash(bytes);
                var sb = new StringBuilder(hash.Length * 2);
                for (var i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
                }
                return sb.ToString();
            }
        }

        private void UpsertLocalCashierCredentialFromBackend(BackendLoginRecord backendUser, string plainPassword)
        {
            if (backendUser == null) return;
            var backendRole = (backendUser.role ?? string.Empty).Trim().ToUpperInvariant();
            if (!IsCachedStaffRole(backendRole)) return;

            var userId = (backendUser.id ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(userId)) return;

            var protectedSecret = ProtectLocalSecret(plainPassword);
            if (string.IsNullOrWhiteSpace(protectedSecret)) return;
            var cachedAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

            lock (_localCashierCredentialsSync)
            {
                NormalizeLocalCashierCredentialsCacheNoLock();

                var target = FindLocalCashierCredentialByUserIdNoLock(userId);

                if (target == null)
                {
                    target = new LocalCashierCredentialRecord();
                    _localCashierCredentials.Entries.Add(target);
                }

                target.UserId = userId;
                target.Username = (backendUser.username ?? string.Empty).Trim();
                target.DisplayName = !string.IsNullOrWhiteSpace(backendUser.display_name)
                    ? backendUser.display_name.Trim()
                    : (backendUser.displayName ?? string.Empty).Trim();
                target.Phone = (backendUser.phone ?? string.Empty).Trim();
                target.Email = (backendUser.email ?? string.Empty).Trim();
                target.Role = backendRole;
                target.IsActive = backendUser.is_active || backendUser.isActive;
                target.PasswordHash = string.Empty;
                target.PasswordSecretProtected = protectedSecret;
                target.UserUpdatedAt = cachedAt;
                target.SecretUpdatedAt = string.Empty;
                target.Dirty = false;
                target.LastSyncedAt = cachedAt;

                SaveCashierCredentialsCacheToDisk(_localCashierCredentials);
            }
        }

        private void RememberLocalCashierCredentialForDeferredSync(string userId, string plainPassword)
        {
            var id = (userId ?? string.Empty).Trim();
            var secret = plainPassword ?? string.Empty;
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(secret)) return;

            var protectedSecret = ProtectLocalSecret(secret);
            if (string.IsNullOrWhiteSpace(protectedSecret)) return;

            lock (_localCashierCredentialsSync)
            {
                NormalizeLocalCashierCredentialsCacheNoLock();
                var target = FindLocalCashierCredentialByUserIdNoLock(id);
                if (target == null) return;

                var changed = false;
                if (!string.IsNullOrWhiteSpace(target.PasswordHash))
                {
                    target.PasswordHash = string.Empty;
                    changed = true;
                }
                if (!string.Equals((target.PasswordSecretProtected ?? string.Empty).Trim(), protectedSecret, StringComparison.Ordinal))
                {
                    target.PasswordSecretProtected = protectedSecret;
                    changed = true;
                }
                if (!target.Dirty)
                {
                    target.Dirty = true;
                    changed = true;
                }

                if (changed)
                {
                    SaveCashierCredentialsCacheToDisk(_localCashierCredentials);
                }
            }
        }

        private int TryPerformLocalCachedLogin(
            string identifier,
            string password,
            out string role,
            out string normalizedUsername,
            out string userId,
            out string backendRole,
            out string error)
        {
            role = null;
            normalizedUsername = null;
            userId = null;
            backendRole = null;
            error = null;

            var ident = (identifier ?? string.Empty).Trim();
            var pass = password ?? string.Empty;
            if (string.IsNullOrWhiteSpace(ident) || string.IsNullOrWhiteSpace(pass))
            {
                error = "invalid_input";
                return -1;
            }

            string incomingHash = null;
            LocalCashierCredentialRecord matched = null;
            var matchedPriority = int.MaxValue;

            lock (_localCashierCredentialsSync)
            {
                NormalizeLocalCashierCredentialsCacheNoLock();
                for (var i = 0; i < _localCashierCredentials.Entries.Count; i++)
                {
                    var row = _localCashierCredentials.Entries[i];
                    if (row == null) continue;
                    if (!row.IsActive) continue;
                    if (!IsCachedStaffRole(row.Role)) continue;
                    if (!LocalIdentifierMatches(row, ident)) continue;

                    var matchesLocalSecret = false;
                    if (!string.IsNullOrWhiteSpace(row.PasswordSecretProtected))
                    {
                        matchesLocalSecret = VerifyProtectedLocalSecret(row.PasswordSecretProtected, pass);
                    }
                    else if (!string.IsNullOrWhiteSpace(row.PasswordHash))
                    {
                        if (string.IsNullOrWhiteSpace(incomingHash)) incomingHash = ComputeMd5Hex(pass);
                        matchesLocalSecret = string.Equals(
                            row.PasswordHash.Trim(),
                            incomingHash,
                            StringComparison.OrdinalIgnoreCase);
                    }

                    if (!matchesLocalSecret) continue;

                    var priority = GetStaffRoleLoginPriority(row.Role);
                    if (matched == null || priority < matchedPriority)
                    {
                        matched = row;
                        matchedPriority = priority;
                    }
                }
            }

            if (matched == null)
            {
                error = "No matching local credential. Check connection or credentials.";
                return -1;
            }

            backendRole = (matched.Role ?? string.Empty).Trim().ToUpperInvariant();
            role = MapBackendRoleToLocalRole(backendRole);
            if (string.Equals(role, "CUSTOMER", StringComparison.OrdinalIgnoreCase))
            {
                error = "This account is CUSTOMER. Use a CASHIER/ADMIN POS account.";
                return -1;
            }

            userId = (matched.UserId ?? string.Empty).Trim();
            normalizedUsername =
                !string.IsNullOrWhiteSpace(matched.DisplayName) ? matched.DisplayName :
                !string.IsNullOrWhiteSpace(matched.Username) ? matched.Username :
                !string.IsNullOrWhiteSpace(matched.Phone) ? matched.Phone :
                ident;

            return 1;
        }

        private void MergeLocalCashierCredentialsFromSupabase(List<SupabaseCashierCredentialRow> rows)
        {
            if (rows == null) return;

            lock (_localCashierCredentialsSync)
            {
                NormalizeLocalCashierCredentialsCacheNoLock();
                var changed = false;
                var syncedAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                var authoritativeUserIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (var i = 0; i < rows.Count; i++)
                {
                    var src = rows[i];
                    if (src == null) continue;

                    var userId = (src.Id ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(userId)) continue;
                    authoritativeUserIds.Add(userId);

                    var role = (src.Role ?? string.Empty).Trim().ToUpperInvariant();
                    if (!IsCachedStaffRole(role)) continue;

                    LocalCashierCredentialRecord target = null;
                    for (var j = 0; j < _localCashierCredentials.Entries.Count; j++)
                    {
                        var row = _localCashierCredentials.Entries[j];
                        if (row == null) continue;
                        if (!EqualsTrimmedIgnoreCase(row.UserId, userId)) continue;
                        target = row;
                        break;
                    }

                    if (target == null)
                    {
                        target = new LocalCashierCredentialRecord();
                        _localCashierCredentials.Entries.Add(target);
                        changed = true;
                    }

                    var displayName = !string.IsNullOrWhiteSpace(src.DisplayName)
                        ? src.DisplayName.Trim()
                        : (src.DisplayNameCamel ?? string.Empty).Trim();

                    if (!EqualsTrimmedIgnoreCase(target.UserId, userId)) { target.UserId = userId; changed = true; }
                    var username = (src.Username ?? string.Empty).Trim();
                    if (!EqualsTrimmedIgnoreCase(target.Username, username)) { target.Username = username; changed = true; }
                    if (!EqualsTrimmedIgnoreCase(target.DisplayName, displayName)) { target.DisplayName = displayName; changed = true; }
                    var phone = (src.Phone ?? string.Empty).Trim();
                    if (!EqualsTrimmedIgnoreCase(target.Phone, phone)) { target.Phone = phone; changed = true; }
                    var email = (src.Email ?? string.Empty).Trim();
                    if (!EqualsTrimmedIgnoreCase(target.Email, email)) { target.Email = email; changed = true; }
                    if (!EqualsTrimmedIgnoreCase(target.Role, role)) { target.Role = role; changed = true; }
                    if (target.IsActive != src.IsActive) { target.IsActive = src.IsActive; changed = true; }

                    var userUpdatedAt = (src.UpdatedAt ?? string.Empty).Trim();
                    if (!EqualsTrimmedIgnoreCase(target.UserUpdatedAt, userUpdatedAt))
                    {
                        target.UserUpdatedAt = userUpdatedAt;
                        changed = true;
                    }

                    var secretUpdatedAt = (src.SecretUpdatedAt ?? string.Empty).Trim();
                    var hadAuthoritativeSecretVersion = !string.IsNullOrWhiteSpace(target.SecretUpdatedAt);
                    var secretVersionChanged = hadAuthoritativeSecretVersion
                        && !EqualsTrimmedIgnoreCase(target.SecretUpdatedAt, secretUpdatedAt);
                    if (secretVersionChanged && !string.IsNullOrWhiteSpace(target.PasswordHash))
                    {
                        // The backend secret changed (or was removed). Drop the local
                        // offline verifier so the cashier must refresh it from a real login.
                        target.PasswordHash = string.Empty;
                        changed = true;
                    }
                    if (secretVersionChanged && !string.IsNullOrWhiteSpace(target.PasswordSecretProtected))
                    {
                        target.PasswordSecretProtected = string.Empty;
                        changed = true;
                    }
                    if (!EqualsTrimmedIgnoreCase(target.SecretUpdatedAt, secretUpdatedAt))
                    {
                        target.SecretUpdatedAt = secretUpdatedAt;
                        changed = true;
                    }
                    if (secretVersionChanged && target.Dirty)
                    {
                        target.Dirty = false;
                        changed = true;
                    }
                    if (!EqualsTrimmedIgnoreCase(target.LastSyncedAt, syncedAt))
                    {
                        target.LastSyncedAt = syncedAt;
                        changed = true;
                    }
                }

                if (authoritativeUserIds.Count > 0)
                {
                    for (var i = _localCashierCredentials.Entries.Count - 1; i >= 0; i--)
                    {
                        var row = _localCashierCredentials.Entries[i];
                        if (row == null) continue;

                        var userId = (row.UserId ?? string.Empty).Trim();
                        if (string.IsNullOrWhiteSpace(userId)) continue;
                        if (!IsCachedStaffRole(row.Role)) continue;
                        if (row.Dirty) continue;
                        if (authoritativeUserIds.Contains(userId)) continue;

                        _localCashierCredentials.Entries.RemoveAt(i);
                        changed = true;
                    }
                }

                if (!changed) return;
                SaveCashierCredentialsCacheToDisk(_localCashierCredentials);
            }
        }

        private List<LocalCashierCredentialRecord> GetDirtyLocalCashierCredentialsSnapshot()
        {
            var list = new List<LocalCashierCredentialRecord>();
            lock (_localCashierCredentialsSync)
            {
                NormalizeLocalCashierCredentialsCacheNoLock();
                for (var i = 0; i < _localCashierCredentials.Entries.Count; i++)
                {
                    var row = _localCashierCredentials.Entries[i];
                    if (row == null || !row.Dirty) continue;
                    if (string.IsNullOrWhiteSpace(row.UserId)) continue;
                    if (string.IsNullOrWhiteSpace(row.PasswordSecretProtected)) continue;
                    if (!IsCachedStaffRole(row.Role)) continue;

                    list.Add(new LocalCashierCredentialRecord
                    {
                        UserId = row.UserId,
                        Username = row.Username,
                        DisplayName = row.DisplayName,
                        Phone = row.Phone,
                        Email = row.Email,
                        Role = row.Role,
                        IsActive = row.IsActive,
                        PasswordHash = row.PasswordHash,
                        PasswordSecretProtected = row.PasswordSecretProtected,
                        UserUpdatedAt = row.UserUpdatedAt,
                        SecretUpdatedAt = row.SecretUpdatedAt,
                        Dirty = row.Dirty,
                        LastSyncedAt = row.LastSyncedAt
                    });
                }
            }
            return list;
        }

        private void MarkLocalCashierCredentialsAsSynced(List<string> userIds)
        {
            if (userIds == null || userIds.Count == 0) return;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < userIds.Count; i++)
            {
                var id = (userIds[i] ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(id)) continue;
                seen.Add(id);
            }
            if (seen.Count == 0) return;

            lock (_localCashierCredentialsSync)
            {
                NormalizeLocalCashierCredentialsCacheNoLock();
                var changed = false;
                var syncedAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                for (var i = 0; i < _localCashierCredentials.Entries.Count; i++)
                {
                    var row = _localCashierCredentials.Entries[i];
                    if (row == null) continue;
                    var id = (row.UserId ?? string.Empty).Trim();
                    if (!seen.Contains(id)) continue;
                    if (row.Dirty)
                    {
                        row.Dirty = false;
                        changed = true;
                    }
                    if (!EqualsTrimmedIgnoreCase(row.LastSyncedAt, syncedAt))
                    {
                        row.LastSyncedAt = syncedAt;
                        changed = true;
                    }
                }
                if (changed) SaveCashierCredentialsCacheToDisk(_localCashierCredentials);
            }
        }

        private string GetCurrentSessionUserId()
        {
            return _currentSession == null ? string.Empty : (_currentSession.UserId ?? string.Empty).Trim();
        }

        private bool HasUsableCurrentSessionToken()
        {
            if (_currentSession == null) return false;

            var sessionToken = (_currentSession.SessionToken ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(sessionToken)) return false;

            var expiresAtMillis = _currentSession.SessionExpiresAtMillis;
            if (expiresAtMillis > 0)
            {
                var nowMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (expiresAtMillis <= nowMillis + 60000) return false;
            }

            return true;
        }

        private string GetCurrentSessionToken()
        {
            if (!HasUsableCurrentSessionToken()) return string.Empty;
            return (_currentSession.SessionToken ?? string.Empty).Trim();
        }

        private string GetCurrentBackendActorRole()
        {
            var backendRole = _currentSession == null ? string.Empty : (_currentSession.BackendRole ?? string.Empty).Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(backendRole)) return backendRole;

            var localRole = _currentSession == null ? string.Empty : (_currentSession.Role ?? string.Empty).Trim().ToUpperInvariant();
            if (localRole == "TAKEAWAY") return "CASHIER";
            if (localRole == "DELIVERY") return "DRIVER";
            if (localRole == "ADMIN") return "ADMIN_POS";
            return localRole;
        }

        private string GetCurrentSessionDisplayName()
        {
            if (_currentSession == null) return string.Empty;
            var username = (_currentSession.Username ?? string.Empty).Trim();
            return username;
        }

        // Backend login now uses the public RPC backed by table-only users storage.
        private int TryPerformBackendLogin(
            string username,
            string password,
            out string role,
            out string normalizedUsername,
            out string userId,
            out string backendRole,
            out string sessionToken,
            out long sessionExpiresAtMillis,
            out string error)
        {
            role = null;
            normalizedUsername = null;
            userId = null;
            backendRole = null;
            sessionToken = null;
            sessionExpiresAtMillis = 0;
            error = null;
            try
            {
                var cfg = LoadSupabaseClientConfigResolved();
                if (cfg == null)
                {
                    error = "Supabase config is missing.";
                    return -1;
                }

                using (var http = new HttpClient { BaseAddress = new Uri(cfg.Url.TrimEnd('/')) })
                {
                    var timeoutMs = cfg.RestTimeoutMs > 0
                        ? Math.Max(cfg.RestTimeoutMs, SupabaseSessionIssueTimeoutMs)
                        : SupabaseSessionIssueTimeoutMs;
                    http.Timeout = TimeSpan.FromMilliseconds(timeoutMs);

                    BackendRoleSessionRpcResponse issuedSession = null;
                    string issueError = null;
                    string resolvedIdentifier = null;
                    var loginIdentifiers = BuildBackendLoginIdentifierCandidates(username);
                    if (loginIdentifiers.Count == 0)
                    {
                        AddUniqueBackendLoginIdentifier(loginIdentifiers, username);
                    }

                    for (var i = 0; i < loginIdentifiers.Count; i++)
                    {
                        var loginIdentifier = loginIdentifiers[i];
                        try
                        {
                            issuedSession = TryIssueBackendRoleSession(http, cfg, loginIdentifier, password, out issueError);
                        }
                        catch (TaskCanceledException ex)
                        {
                            AppendPosRuntimeLog("TryIssueBackendRoleSession", ex);
                            issueError = "backend_unreachable";
                        }
                        catch (HttpRequestException ex)
                        {
                            AppendPosRuntimeLog("TryIssueBackendRoleSession", ex);
                            issueError = "backend_unreachable";
                        }

                        if (issuedSession != null && issuedSession.ok && issuedSession.user != null)
                        {
                            resolvedIdentifier = loginIdentifier;
                            break;
                        }
                    }
                    if (issuedSession == null || !issuedSession.ok || issuedSession.user == null)
                    {
                        error = string.IsNullOrWhiteSpace(issueError) ? "invalid_credentials" : issueError;
                        if (string.Equals(error, "backend_unreachable", StringComparison.OrdinalIgnoreCase)
                            || error.StartsWith("http_5", StringComparison.OrdinalIgnoreCase))
                        {
                            return 0;
                        }
                        return -1;
                    }

                    var matched = issuedSession.user;
                    sessionToken = (issuedSession.sessionToken ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(sessionToken))
                    {
                        error = "session_token_missing";
                        return -1;
                    }
                    sessionExpiresAtMillis = issuedSession.expiresAtMillis > 0
                        ? issuedSession.expiresAtMillis
                        : 0;

                    backendRole = (matched.role ?? string.Empty).Trim().ToUpperInvariant();
                    role = MapBackendRoleToLocalRole(backendRole);
                    userId = (matched.id ?? string.Empty).Trim();

                    normalizedUsername =
                        !string.IsNullOrWhiteSpace(matched.display_name) ? matched.display_name :
                        !string.IsNullOrWhiteSpace(matched.displayName) ? matched.displayName :
                        !string.IsNullOrWhiteSpace(matched.username) ? matched.username :
                        !string.IsNullOrWhiteSpace(matched.email) ? matched.email :
                        !string.IsNullOrWhiteSpace(matched.phone) ? matched.phone :
                        (!string.IsNullOrWhiteSpace(resolvedIdentifier) ? resolvedIdentifier : username);

                    if (string.Equals(role, "CUSTOMER", StringComparison.OrdinalIgnoreCase))
                    {
                        error = "This account is CUSTOMER. Use a CASHIER/ADMIN POS account.";
                        return -1;
                    }

                    if (!string.IsNullOrWhiteSpace(resolvedIdentifier)
                        && !string.Equals((resolvedIdentifier ?? string.Empty).Trim(), (username ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        AppendPosRuntimeLog("TryPerformBackendLogin", "Resolved login identifier via local staff cache.");
                    }

                    UpsertLocalCashierCredentialFromBackend(matched, password);
                }

                if (string.IsNullOrEmpty(role)) role = "CUSTOMER";
                return 1;
            }
            catch (TaskCanceledException ex)
            {
                AppendPosRuntimeLog("TryPerformBackendLogin", ex);
                error = "backend_unreachable";
                return 0;
            }
            catch (HttpRequestException ex)
            {
                AppendPosRuntimeLog("TryPerformBackendLogin", ex);
                error = "backend_unreachable";
                return 0;
            }
            catch (Exception ex)
            {
                AppendPosRuntimeLog("TryPerformBackendLogin", ex);
                error = ex.Message;
                return -1;
            }
        }

        private BackendRoleSessionRpcResponse TryIssueBackendRoleSession(HttpClient http, SupabaseClientConfigRecord cfg, string identifier, string password, out string error)
        {
            error = null;
            if (http == null || cfg == null)
            {
                error = "supabase_config_missing";
                return null;
            }

            using (var loginReq = new HttpRequestMessage(HttpMethod.Post, "/rest/v1/rpc/api_issue_role_session"))
            {
                loginReq.Headers.TryAddWithoutValidation("apikey", cfg.AnonKey);
                loginReq.Headers.TryAddWithoutValidation("Authorization", "Bearer " + cfg.AnonKey);
                loginReq.Headers.Accept.ParseAdd("application/json");
                var rpcPayload = SerializeJson(new BackendVerifyPhonePasswordRequest
                {
                    Phone = identifier ?? string.Empty,
                    Password = password ?? string.Empty,
                    TtlHours = 24 * 14
                });
                loginReq.Content = new StringContent(rpcPayload, Encoding.UTF8, "application/json");

                System.Net.Http.HttpResponseMessage loginResp;
                string body = string.Empty;
                try
                {
                    loginResp = http.SendAsync(loginReq).GetAwaiter().GetResult();
                    body = loginResp.Content == null ? string.Empty : loginResp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
                catch (System.Net.Http.HttpRequestException ex)
                {
                    System.Diagnostics.Debug.WriteLine("Network failure during login: " + ex.Message);
                    error = "backend_unreachable";
                    return null;
                }
                catch (System.Net.WebException ex)
                {
                    System.Diagnostics.Debug.WriteLine("WebException during login: " + ex.Message);
                    error = "backend_unreachable";
                    return null;
                }
                catch (System.Net.Sockets.SocketException ex)
                {
                    System.Diagnostics.Debug.WriteLine("SocketException during login: " + ex.Message);
                    error = "backend_unreachable";
                    return null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception during login: " + ex.Message);
                    error = "backend_unreachable";
                    return null;
                }

                if (!loginResp.IsSuccessStatusCode)
                {
                    var serverMessage = ExtractServerErrorMessage(body);
                    error = "http_" + (int)loginResp.StatusCode
                        + (string.IsNullOrWhiteSpace(serverMessage) ? string.Empty : ":" + serverMessage);
                    return null;
                }

                var rpc = DeserializeJson<BackendRoleSessionRpcResponse>(body);
                if (rpc == null)
                {
                    error = ExtractServerErrorMessage(body);
                    return null;
                }

                if (!rpc.ok || rpc.user == null || !(rpc.user.is_active || rpc.user.isActive))
                {
                    var rpcError = (rpc.error ?? string.Empty).Trim();
                    error = string.IsNullOrWhiteSpace(rpcError) ? "invalid_credentials" : rpcError;
                    return null;
                }

                if (string.IsNullOrWhiteSpace(rpc.sessionToken))
                {
                    error = "session_token_missing";
                    return null;
                }

                return rpc;
            }
        }

        private BackendLoginRecord TryVerifyBackendCredentials(HttpClient http, SupabaseClientConfigRecord cfg, string identifier, string password, out string error)
        {
            var issued = TryIssueBackendRoleSession(http, cfg, identifier, password, out error);
            if (issued == null || !issued.ok || issued.user == null)
            {
                return null;
            }

            var matched = issued.user;
            if (!(matched.is_active || matched.isActive))
            {
                error = "inactive_account";
                return null;
            }

            return matched;
        }

        private BackendLoginRecord TryResolveBackendStaffLogin(HttpClient http, SupabaseClientConfigRecord cfg, string identifier, string password, out string error)
        {
            error = null;
            if (http == null || cfg == null)
            {
                error = "supabase_config_missing";
                return null;
            }

            var ident = (identifier ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(ident))
            {
                error = "invalid_identifier";
                return null;
            }

            var preferredRoles = new[] { "CASHIER", "ADMIN_POS", "ADMIN", "DRIVER" };
            for (var i = 0; i < preferredRoles.Length; i++)
            {
                var role = preferredRoles[i];
                var rows = SearchUsersByRole(http, cfg, ident, role, 10, out var searchError);
                if (rows == null || rows.Count == 0)
                {
                    if (!string.IsNullOrWhiteSpace(searchError)) error = searchError;
                    continue;
                }

                for (var r = 0; r < rows.Count; r++)
                {
                    var candidate = rows[r];
                    if (candidate == null) continue;
                    if (!(candidate.is_active || candidate.isActive)) continue;

                    var probes = new List<string>();
                    if (!string.IsNullOrWhiteSpace(candidate.username)) probes.Add(candidate.username.Trim());
                    if (!string.IsNullOrWhiteSpace(candidate.phone)) probes.Add(candidate.phone.Trim());
                    if (!string.IsNullOrWhiteSpace(candidate.email)) probes.Add(candidate.email.Trim());
                    if (!string.IsNullOrWhiteSpace(candidate.id)) probes.Add(candidate.id.Trim());

                    for (var p = 0; p < probes.Count; p++)
                    {
                        var probe = probes[p];
                        var verified = TryVerifyBackendCredentials(http, cfg, probe, password, out var verifyError);
                        if (verified == null)
                        {
                            if (!string.IsNullOrWhiteSpace(verifyError)) error = verifyError;
                            continue;
                        }

                        var backendRole = (verified.role ?? string.Empty).Trim().ToUpperInvariant();
                        if (backendRole == "CASHIER" || backendRole == "ADMIN_POS" || backendRole == "ADMIN" || backendRole == "DRIVER")
                        {
                            return verified;
                        }
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(error)) error = "staff_not_found";
            return null;
        }

        private static bool IsMissingSecretBackendError(string error)
        {
            var normalized = (error ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalized)) return false;
            return normalized.Contains("no_secret")
                   || normalized.Contains("no_password")
                   || normalized.Contains("password hash")
                   || normalized.Contains("no secret")
                   || normalized.Contains("password_reset_required")
                   || normalized.Contains("password_hash_unsupported");
        }

        private BackendLoginRecord TryRepairMissingBackendSecret(
            HttpClient http,
            SupabaseClientConfigRecord cfg,
            string identifier,
            string password,
            out string error)
        {
            error = null;
            if (http == null || cfg == null)
            {
                error = "supabase_config_missing";
                return null;
            }

            var ident = (identifier ?? string.Empty).Trim();
            var pass = password ?? string.Empty;
            if (string.IsNullOrWhiteSpace(ident) || string.IsNullOrWhiteSpace(pass))
            {
                error = "invalid_input";
                return null;
            }

            var candidate = TryFindSingleStaffCandidateForSecretRepair(http, cfg, ident, out var findError);
            if (candidate == null)
            {
                error = string.IsNullOrWhiteSpace(findError) ? "no_secret" : findError;
                return null;
            }

            var candidateId = (candidate.id ?? string.Empty).Trim();
            var candidateRole = (candidate.role ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(candidateId) || !IsCachedStaffRole(candidateRole))
            {
                error = "staff_not_found";
                return null;
            }

            var upsertPayload = new SupabaseUpsertStaffCredentialRequest
            {
                UserId = candidateId,
                Username = (candidate.username ?? string.Empty).Trim(),
                DisplayName = !string.IsNullOrWhiteSpace(candidate.display_name)
                    ? candidate.display_name.Trim()
                    : (candidate.displayName ?? string.Empty).Trim(),
                Phone = (candidate.phone ?? string.Empty).Trim(),
                Email = (candidate.email ?? string.Empty).Trim(),
                Role = candidateRole,
                IsActive = candidate.is_active || candidate.isActive,
                Password = pass,
                Pin = candidateRole == "CASHIER" ? pass : null
            };

            var upsertRaw = PostSupabaseRpcGetStringAsync(
                cfg,
                "api_pos_upsert_staff_credential",
                SerializeJson(upsertPayload),
                actorRoleOverride: "ADMIN_POS",
                actorUserIdOverride: candidateId).GetAwaiter().GetResult();

            if (string.IsNullOrWhiteSpace(upsertRaw))
            {
                error = "no_secret";
                return null;
            }

            var upsertResp = DeserializeJson<SupabaseUpsertStaffCredentialResponse>(upsertRaw);
            if (upsertResp != null && !upsertResp.Ok)
            {
                var rpcError = (upsertResp.Error ?? string.Empty).Trim();
                error = string.IsNullOrWhiteSpace(rpcError) ? "no_secret" : rpcError;
                return null;
            }
            if (upsertResp == null && upsertRaw.IndexOf("\"ok\":true", StringComparison.OrdinalIgnoreCase) < 0)
            {
                var rawError = ExtractServerErrorMessage(upsertRaw);
                error = string.IsNullOrWhiteSpace(rawError) ? "no_secret" : rawError;
                return null;
            }

            var retryProbes = new List<string>();
            if (!string.IsNullOrWhiteSpace(candidate.username)) retryProbes.Add(candidate.username.Trim());
            if (!string.IsNullOrWhiteSpace(candidate.phone)) retryProbes.Add(candidate.phone.Trim());
            if (!string.IsNullOrWhiteSpace(candidate.email)) retryProbes.Add(candidate.email.Trim());
            if (!string.IsNullOrWhiteSpace(candidate.id)) retryProbes.Add(candidate.id.Trim());
            retryProbes.Add(ident);

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < retryProbes.Count; i++)
            {
                var probe = (retryProbes[i] ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(probe)) continue;
                if (!seen.Add(probe)) continue;
                var verified = TryVerifyBackendCredentials(http, cfg, probe, pass, out var verifyError);
                if (verified != null) return verified;
                if (!string.IsNullOrWhiteSpace(verifyError)) error = verifyError;
            }

            if (string.IsNullOrWhiteSpace(error)) error = "no_secret";
            return null;
        }

        private BackendLoginRecord TryFindSingleStaffCandidateForSecretRepair(
            HttpClient http,
            SupabaseClientConfigRecord cfg,
            string identifier,
            out string error)
        {
            error = null;
            var ident = (identifier ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(ident))
            {
                error = "invalid_identifier";
                return null;
            }

            var preferredRoles = new[] { "ADMIN_POS", "ADMIN", "CASHIER", "DRIVER" };
            var matches = new List<BackendLoginRecord>();
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < preferredRoles.Length; i++)
            {
                var role = preferredRoles[i];
                var rows = SearchUsersByRole(http, cfg, ident, role, 20, out var searchError);
                if (rows == null || rows.Count == 0)
                {
                    if (!string.IsNullOrWhiteSpace(searchError)) error = searchError;
                    continue;
                }

                for (var r = 0; r < rows.Count; r++)
                {
                    var row = rows[r];
                    if (row == null) continue;
                    if (!(row.is_active || row.isActive)) continue;
                    var backendRole = (row.role ?? string.Empty).Trim().ToUpperInvariant();
                    if (!IsCachedStaffRole(backendRole)) continue;
                    if (!BackendRecordMatchesIdentifier(row, ident)) continue;

                    var id = (row.id ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(id)) continue;
                    if (!seenIds.Add(id)) continue;

                    matches.Add(row);
                }
            }

            if (matches.Count == 1) return matches[0];
            if (matches.Count > 1)
            {
                error = "staff_identifier_ambiguous";
                return null;
            }

            if (string.IsNullOrWhiteSpace(error)) error = "staff_not_found";
            return null;
        }

        private static bool BackendRecordMatchesIdentifier(BackendLoginRecord row, string identifier)
        {
            if (row == null || string.IsNullOrWhiteSpace(identifier)) return false;
            var ident = identifier.Trim();
            return EqualsTrimmedIgnoreCase(row.id, ident)
                   || EqualsTrimmedIgnoreCase(row.username, ident)
                   || EqualsTrimmedIgnoreCase(row.phone, ident)
                   || EqualsTrimmedIgnoreCase(row.email, ident)
                   || EqualsTrimmedIgnoreCase(row.display_name, ident)
                   || EqualsTrimmedIgnoreCase(row.displayName, ident);
        }

        private List<BackendLoginRecord> SearchUsersByRole(HttpClient http, SupabaseClientConfigRecord cfg, string identifier, string role, int limit, out string error)
        {
            error = null;
            if (http == null || cfg == null) return new List<BackendLoginRecord>();

            using (var req = new HttpRequestMessage(HttpMethod.Post, "/rest/v1/rpc/api_users_search"))
            {
                req.Headers.TryAddWithoutValidation("apikey", cfg.AnonKey);
                req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + cfg.AnonKey);
                req.Headers.TryAddWithoutValidation("x-fale7-role", "ADMIN_POS");
                req.Headers.TryAddWithoutValidation("x_fale7_role", "ADMIN_POS");
                req.Headers.Accept.ParseAdd("application/json");
                req.Content = new StringContent(SerializeJson(new BackendUsersSearchRequest
                {
                    Identifier = identifier ?? string.Empty,
                    Role = role ?? string.Empty,
                    Limit = limit <= 0 ? 10 : limit
                }), Encoding.UTF8, "application/json");

                var resp = http.SendAsync(req).GetAwaiter().GetResult();
                var raw = resp.Content == null ? string.Empty : resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (!resp.IsSuccessStatusCode)
                {
                    var serverMessage = ExtractServerErrorMessage(raw);
                    error = "http_" + (int)resp.StatusCode
                        + (string.IsNullOrWhiteSpace(serverMessage) ? string.Empty : ":" + serverMessage);
                    return new List<BackendLoginRecord>();
                }

                return DeserializeJson<List<BackendLoginRecord>>(raw) ?? new List<BackendLoginRecord>();
            }
        }

        private static string ExtractServerErrorMessage(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            var trimmed = raw.Trim();
            if (trimmed.Length == 0) return string.Empty;
            try
            {
                // Works for common Supabase error bodies: {"message":"..."} / {"error":"..."}
                var keys = new[] { "\"message\"", "\"error\"", "\"details\"" };
                for (int i = 0; i < keys.Length; i++)
                {
                    var keyIndex = trimmed.IndexOf(keys[i], StringComparison.OrdinalIgnoreCase);
                    if (keyIndex < 0) continue;
                    var colonIndex = trimmed.IndexOf(':', keyIndex);
                    if (colonIndex < 0) continue;
                    var firstQuote = trimmed.IndexOf('"', colonIndex + 1);
                    if (firstQuote < 0) continue;
                    var secondQuote = trimmed.IndexOf('"', firstQuote + 1);
                    if (secondQuote <= firstQuote) continue;
                    var value = trimmed.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                    if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
                }
            }
            catch
            {
            }
            return trimmed.Length > 220 ? trimmed.Substring(0, 220) : trimmed;
        }

        // records used in Supabase HTTP responses
        [DataContract]
        private sealed class BackendLoginRecord
        {
            [DataMember(Name = "id", EmitDefaultValue = false)]
            public string id { get; set; }

            [DataMember(Name = "username", EmitDefaultValue = false)]
            public string username { get; set; }

            [DataMember(Name = "phone", EmitDefaultValue = false)]
            public string phone { get; set; }

            [DataMember(Name = "email", EmitDefaultValue = false)]
            public string email { get; set; }

            [DataMember(Name = "display_name", EmitDefaultValue = false)]
            public string display_name { get; set; }

            [DataMember(Name = "displayName", EmitDefaultValue = false)]
            public string displayName { get; set; }

            [DataMember(Name = "role", EmitDefaultValue = false)]
            public string role { get; set; }

            [DataMember(Name = "is_active", EmitDefaultValue = false)]
            public bool is_active { get; set; }

            [DataMember(Name = "isActive", EmitDefaultValue = false)]
            public bool isActive { get; set; }
        }

        [DataContract]
        private sealed class BackendLoginRpcResponse
        {
            [DataMember(Name = "ok", EmitDefaultValue = false)]
            public bool ok { get; set; }

            [DataMember(Name = "error", EmitDefaultValue = false)]
            public string error { get; set; }

            [DataMember(Name = "user", EmitDefaultValue = false)]
            public BackendLoginRecord user { get; set; }
        }

        [DataContract]
        private sealed class BackendRoleSessionRpcResponse
        {
            [DataMember(Name = "ok", EmitDefaultValue = false)]
            public bool ok { get; set; }

            [DataMember(Name = "error", EmitDefaultValue = false)]
            public string error { get; set; }

            [DataMember(Name = "sessionToken", EmitDefaultValue = false)]
            public string sessionToken { get; set; }

            [DataMember(Name = "expiresAtMillis", EmitDefaultValue = false)]
            public long expiresAtMillis { get; set; }

            [DataMember(Name = "user", EmitDefaultValue = false)]
            public BackendLoginRecord user { get; set; }
        }

        [DataContract]
        private sealed class BackendVerifyPhonePasswordRequest
        {
            [DataMember(Name = "p_phone")]
            public string Phone { get; set; }

            [DataMember(Name = "p_password")]
            public string Password { get; set; }

            [DataMember(Name = "p_ttl_hours", EmitDefaultValue = false)]
            public int? TtlHours { get; set; }
        }

        [DataContract]
        private sealed class BackendUsersSearchRequest
        {
            [DataMember(Name = "p_identifier", EmitDefaultValue = false)]
            public string Identifier { get; set; }

            [DataMember(Name = "p_role", EmitDefaultValue = false)]
            public string Role { get; set; }

            [DataMember(Name = "p_limit", EmitDefaultValue = false)]
            public int Limit { get; set; }
        }

        private static bool SupabaseTokenLooksLikeJwt(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;
            var parts = token.Trim().Split('.');
            return parts.Length == 3
                   && !string.IsNullOrWhiteSpace(parts[0])
                   && !string.IsNullOrWhiteSpace(parts[1])
                   && !string.IsNullOrWhiteSpace(parts[2]);
        }

        private void AppendPosRuntimeLog(string scope, Exception ex)
        {
            var details = ex == null ? "unknown error" : ex.ToString();
            AppendPosRuntimeLog(scope, details);
        }

        private void AppendPosRuntimeLog(string scope, string details)
        {
            try
            {
                EnsureAppDataDir();
                var path = Path.Combine(AppDataLogsDirPath, "pos_runtime.log");
                var line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)
                    + " [" + (scope ?? "unknown") + "] "
                    + (details ?? string.Empty);
                File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
            }
        }

        private sealed class BackendLoginAttemptResult
        {
            public int Attempt { get; set; }
            public bool UsedLocalCache { get; set; }
            public string Role { get; set; }
            public string Username { get; set; }
            public string UserId { get; set; }
            public string BackendRole { get; set; }
            public string SessionToken { get; set; }
            public long SessionExpiresAtMillis { get; set; }
            public string Error { get; set; }
        }

        private bool NavigateToRole(string role, string username)
        {
            var upper = (role ?? string.Empty).Trim().ToUpperInvariant();
            if (upper == "TAKEAWAY")
            {
                _isAdminMode = false;
                ShowTakeawayView(username);
                _ = Task.Run(async () => {
                });
                return true;
            }
            if (upper == "ADMIN")
            {
                _isAdminMode = true;
                ShowTakeawayView(username);
                _ = Task.Run(async () => {
                    await InitRealtimeAsync().ConfigureAwait(false);
                });
                return true;
            }
            if (upper == "DELIVERY")
            {
                _isAdminMode = false;
                ShowDeliveryView(username);
                _ = Task.Run(async () => {
                });
                return true;
            }
            return false;
        }

        private void ShowLoginView()
        {
            CloseAdminReorderPopups();
            CloseAdminAddCategoryForm();
            CloseAdminAddItemForm();
            LoginView.Visibility = Visibility.Visible;
            PlaceholderView.Visibility = Visibility.Collapsed;
            TakeawayView.Visibility = Visibility.Collapsed;
            if (DeliveryView != null) DeliveryView.Visibility = Visibility.Collapsed;
            LoginErrorText.Text = string.Empty;
            LoginPasswordBox.Password = string.Empty;
            ResetVirtualPrinterActivationBuffer();
            Keyboard.ClearFocus();
            Focus();
        }

        private void ShowPlaceholderView(string title, string message, string username)
        {
            LoginView.Visibility = Visibility.Collapsed;
            PlaceholderView.Visibility = Visibility.Visible;
            TakeawayView.Visibility = Visibility.Collapsed;
            if (DeliveryView != null) DeliveryView.Visibility = Visibility.Collapsed;
            PlaceholderTitleText.Text = title;
            PlaceholderMessageText.Text = message;
            PlaceholderUserText.Text = "Logged in as " + (username ?? string.Empty);
        }

        private void ShowTakeawayView(string username)
        {
            LoginView.Visibility = Visibility.Collapsed;
            PlaceholderView.Visibility = Visibility.Collapsed;
            TakeawayView.Visibility = Visibility.Visible;
            if (DeliveryView != null) DeliveryView.Visibility = Visibility.Collapsed;
            TakeawayHeaderUserRun.Text = "\u0643\u0627\u0634\u064A\u0631";
            if (string.IsNullOrEmpty(_activeCategory) && _menu.Count > 0) _activeCategory = _menu[0].Name;
            NormalizeTakeawayState();
            EnsureDeliveryInitialized();
            UpdateRoleSwitchButtonsAndAdminUi();
            RenderDeliveryAppOrderCounters();
            RenderTakeawayAll();
        }

        private void LogoutCurrentUser()
        {
            _ = ReleaseAllLocksForThisDeviceAsync();
            _ = DisconnectRealtimeAsync();
            DeleteSessionFile();
            _currentSession = null;
            CloseQtyPad();
            ShowLoginView();
        }

        private void PlaceholderLogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LogoutCurrentUser();
        }

        private void TakeawayLogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LogoutCurrentUser();
        }

        private void DeliveryLogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LogoutCurrentUser();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (TryHandleVirtualPrinterActivationKey(e))
            {
                e.Handled = true;
                return;
            }
            if (HandleQtyOverlayKey(e))
            {
                e.Handled = true;
                return;
            }
            if (HandleAdminOverlayKey(e))
            {
                e.Handled = true;
                return;
            }
            if (HandleDeliveryNotesOverlayKey(e))
            {
                e.Handled = true;
                return;
            }
        }

        private bool TryHandleVirtualPrinterActivationKey(KeyEventArgs e)
        {
            if (e == null || _virtualPrinterMode || !IsLoginViewVisible() || IsEditableInputFocused()) return false;

            var digit = GetVirtualPrinterActivationDigit(e.Key);
            if (digit.HasValue)
            {
                AppendVirtualPrinterActivationDigit(digit.Value);
                return true;
            }

            if (e.Key == Key.Back)
            {
                if (_virtualPrinterActivationBuffer.Length > 0)
                {
                    _virtualPrinterActivationBuffer.Length -= 1;
                    return true;
                }
                return false;
            }

            if (e.Key == Key.Escape)
            {
                ResetVirtualPrinterActivationBuffer();
                return false;
            }

            if (_virtualPrinterActivationBuffer.Length > 0)
            {
                ResetVirtualPrinterActivationBuffer();
            }

            return false;
        }

        private static char? GetVirtualPrinterActivationDigit(Key key)
        {
            if (key >= Key.D0 && key <= Key.D9) return (char)('0' + (key - Key.D0));
            if (key >= Key.NumPad0 && key <= Key.NumPad9) return (char)('0' + (key - Key.NumPad0));
            return null;
        }

        private void AppendVirtualPrinterActivationDigit(char digit)
        {
            _virtualPrinterActivationBuffer.Append(digit);
            var extra = _virtualPrinterActivationBuffer.Length - VirtualPrinterActivationCode.Length;
            if (extra > 0)
            {
                _virtualPrinterActivationBuffer.Remove(0, extra);
            }

            if (string.Equals(_virtualPrinterActivationBuffer.ToString(), VirtualPrinterActivationCode, StringComparison.Ordinal))
            {
                ActivateVirtualPrinterMode();
            }
        }

        private void ResetVirtualPrinterActivationBuffer()
        {
            _virtualPrinterActivationBuffer.Clear();
        }

        private bool IsLoginViewVisible()
        {
            return LoginView != null && LoginView.Visibility == Visibility.Visible;
        }

        private bool IsEditableInputFocused()
        {
            var focused = Keyboard.FocusedElement as DependencyObject;
            return focused is System.Windows.Controls.Primitives.TextBoxBase
                   || focused is System.Windows.Controls.PasswordBox
                   || focused is System.Windows.Controls.ComboBox;
        }

        private void ActivateVirtualPrinterMode()
        {
            ResetVirtualPrinterActivationBuffer();
            if (_virtualPrinterMode) return;

            _virtualPrinterMode = true;
            Title = "Fale7_POS - Virtual Printer";
            AppendPosRuntimeLog("VirtualPrinter", "Secret activation code accepted; preview-only printing enabled.");
            MessageBox.Show("تم تفعيل وضع المعاينة للطباعة.", "Fale7 POS", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            DismissSupabaseToastFromGlobalClick();
        }

        private void BtnCallDrivers_Click(object sender, RoutedEventArgs e)
        {
            SendDriverCallNotification();
        }

        private void DeliveryBtnCallDrivers_Click(object sender, RoutedEventArgs e)
        {
            SendDriverCallNotification();
        }

        private void SendDriverCallNotification()
        {
            if (_currentSession == null) return;

            _ = Task.Run(async () =>
            {
                try
                {
                    var cfg = LoadSupabaseClientConfigResolved();
                    if (cfg == null)
                    {
                        _ = Dispatcher.BeginInvoke(new Action(delegate
                        {
                            ShowSupabaseToast("خطأ", "لم يتم العثور على إعدادات الاتصال بـ Supabase", false);
                        }), DispatcherPriority.Background);
                        return;
                    }

                    await QueueSupabaseDriverCallNotificationAsync(cfg);
                    
                    _ = Dispatcher.BeginInvoke(new Action(delegate
                    {
                        ShowSupabaseToast("تم ✓", "تم إرسال استدعاء للسائقين بنجاح", false);
                    }), DispatcherPriority.Background);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Driver call notification failed: " + ex.Message);
                    _ = Dispatcher.BeginInvoke(new Action(delegate
                    {
                        ShowSupabaseToast("خطأ", "فشل إرسال الاستدعاء: " + ex.Message, false);
                    }), DispatcherPriority.Background);
                }
            });
        }

        private async Task QueueSupabaseDriverCallNotificationAsync(SupabaseClientConfigRecord cfg)
        {
            if (cfg == null) return;

            await InsertSupabaseNotificationAsync(cfg, new SupabaseNotificationInsertRecord
            {
                RoleTarget = "DRIVER",
                Title = "استدعاء سائقين 📍",
                Message = "يرجى التوجه إلى المطعم فوراً للتقاط الطلبات الجديدة",
                UserId = null,
                OrderId = null,
                OrderType = null,
                Read = false
            });
        }

        private void TakeawaySwitchToDeliveryButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSession == null) return;
            ShowDeliveryView(_currentSession.Username);
        }

        private void DeliverySwitchToTakeawayButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSession == null) return;
            ShowTakeawayView(_currentSession.Username);
        }

        private void UpdateRoleSwitchButtonsAndAdminUi()
        {
            if (TakeawayHeaderInfoText != null)
            {
                TakeawayHeaderInfoText.Visibility = Visibility.Visible;
            }
            if (DeliveryHeaderInfoText != null)
            {
                DeliveryHeaderInfoText.Visibility = Visibility.Visible;
            }
            if (TakeawayHeaderSwitchButton != null) TakeawayHeaderSwitchButton.Visibility = Visibility.Visible;
            if (DeliveryHeaderSwitchButton != null) DeliveryHeaderSwitchButton.Visibility = Visibility.Visible;

            var adminVis = _isAdminMode ? Visibility.Visible : Visibility.Collapsed;
            if (TakeawayAddCategoryButton != null) TakeawayAddCategoryButton.Visibility = adminVis;
            if (TakeawayAddItemButton != null) TakeawayAddItemButton.Visibility = adminVis;
            if (DeliveryAddCategoryButton != null) DeliveryAddCategoryButton.Visibility = adminVis;
            if (DeliveryAddItemButton != null) DeliveryAddItemButton.Visibility = adminVis;

            if (!_isAdminMode)
            {
                CloseAdminReorderPopups();
                CloseAdminAddCategoryForm();
                CloseAdminAddItemForm();
            }
        }

        private void RefreshSharedPickupPanels()
        {
            if (TakeawayView != null && TakeawayView.Visibility == Visibility.Visible) RenderPickups();
            if (DeliveryView != null && DeliveryView.Visibility == Visibility.Visible) RenderDeliverySharedPickups();
        }

        private void RefreshMenuPanelsAcrossRoles()
        {
            if (TakeawayView != null)
            {
                RenderCategories();
                RenderItems();
            }
            if (DeliveryView != null)
            {
                RenderDeliveryCategories();
                RenderDeliveryItems();
            }
        }

        // =====================================================================
        // Shift integration removed.
        // =====================================================================
        private void DeliveryShiftCloseButton_Click(object sender, RoutedEventArgs e)
        {
            return;
        }

        private void UpdateShiftStatusUi()
        {
            return;
        }

        private void CloseOrderEditDialog()
        {
            // يُطلق من PosRealtimeSync عند قفل الأوردر من جهاز آخر
            // أضف هنا إغلاق شاشة التعديل إذا كانت مفتوحة
        }

        private void RefreshOrderEditButtonsForOrder(string orderId)
        {
            QueueSupabaseAppOrdersUiRefresh();
        }

        private void RemoveOrderFromAllLocalLists(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId)) return;
            for (int i = _deliveryOrders.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_deliveryOrders[i]?.Id, orderId, StringComparison.OrdinalIgnoreCase))
                    _deliveryOrders.RemoveAt(i);
            }
            for (int i = _deliveryShiftOrders.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_deliveryShiftOrders[i]?.Id, orderId, StringComparison.OrdinalIgnoreCase))
                    _deliveryShiftOrders.RemoveAt(i);
            }
            QueueSupabaseAppOrdersUiRefresh();
        }

        private void RenderTakeawayPickupQueue()
        {
            try { RefreshSharedPickupPanels(); } catch { }
        }
    }
}
