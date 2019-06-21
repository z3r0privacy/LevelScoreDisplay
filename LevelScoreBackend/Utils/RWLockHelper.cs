using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LevelScoreBackend.Utils
{
    public class RWLockHelper : IDisposable
    {
        public enum LockMode
        {
            Read,UpgradeableRead,Write
        }

        private ReaderWriterLockSlim _lock;
        private LockMode _mode;
        private Action _dispose;
        private ILogger<RWLockHelper> _logger;
        private string _pre;

        public RWLockHelper(ReaderWriterLockSlim @lock, LockMode mode)
        {
            _logger = Program.ServiceProvider.GetRequiredService<ILogger<RWLockHelper>>();

            _dispose = () => { };

            _lock = @lock;
            _mode = mode;

            _pre = $"[{Thread.CurrentThread.ManagedThreadId}: {_lock.GetTag()}] ";

            EnterLock();
        }

        private void EnterLock()
        {
            if (_mode == LockMode.Read)
            {
                _logger.LogTrace(_pre + "Requested ReadLock");

                if (!_lock.IsReadLockHeld && !_lock.IsWriteLockHeld && !_lock.IsUpgradeableReadLockHeld)
                {
                    _logger.LogTrace(_pre + "Acquiring Readlock");

                    _dispose = () => { _lock.ExitReadLock(); };
                    _lock.EnterReadLock();
                }
                else
                {
                    _logger.LogTrace(_pre + "Already holds {lock}", _lock.GetCurrentHeldLocks());
                }
            }
            else if (_mode == LockMode.UpgradeableRead)
            {
                _logger.LogTrace(_pre + "Requested UpgradeableReadLock");

                if (_lock.IsReadLockHeld)
                {
                    var ex = new InvalidOperationException("The thread currently holds a readlock that prevents acquiring a upgradeable lock");
                    _logger.LogError(ex, _pre + "Cannot acquire UpgradeableReadLock");
                    throw ex;
                }
                else if (!_lock.IsUpgradeableReadLockHeld && !_lock.IsWriteLockHeld)
                {
                    _logger.LogTrace(_pre + "Acquiring UpgradeableReadLock");
                    _dispose = () => _lock.ExitUpgradeableReadLock();
                    _lock.EnterUpgradeableReadLock();
                }
                else
                {
                    _logger.LogTrace(_pre + "Already holds {lock}", _lock.GetCurrentHeldLocks());
                }
            }
            else if (_mode == LockMode.Write)
            {
                _logger.LogTrace(_pre + "Requested WriteLock");

                if (!_lock.IsReadLockHeld && !_lock.IsWriteLockHeld && !_lock.IsUpgradeableReadLockHeld)
                {
                    _logger.LogTrace(_pre + "Acquiring WriteLock");
                    _dispose = () => _lock.ExitWriteLock();
                    _lock.EnterWriteLock();
                }
                else if (_lock.IsReadLockHeld)
                {
                    var ex = new InvalidOperationException("The thread currently holds a readlock that is not upgradeable");
                    _logger.LogError(ex, _pre + "Cannot acquire WriteLock");
                    throw ex;
                }
                else if (_lock.IsUpgradeableReadLockHeld && !_lock.IsWriteLockHeld)
                {
                    _logger.LogTrace(_pre + "Upgrading UpgradeableReadLock to WriteLock");
                    _dispose = () => _lock.ExitWriteLock();
                    _lock.EnterWriteLock();
                }
                else
                {
                    _logger.LogTrace(_pre + "Already holds {lock}", _lock.GetCurrentHeldLocks());
                }
            }
            else
            {
                var ex = new ArgumentException("Lockmode must either be Read, UpgradeableRead or Write", "mode");
                _logger.LogError(ex, _pre + "Requested mode {mode}", _mode);
                throw ex;
            }
        }

        public void Dispose()
        {
            _logger.LogTrace(_pre + "Disposing");
            _dispose();
        }
    }
}
