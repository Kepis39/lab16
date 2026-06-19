using lab16.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using System.Windows.Threading;

namespace lab16.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _inputArray = "5 3 8 1 9 2 7 4 6";
        private string _searchNumber = "7";
        private string _sortedArray = "";
        private string _searchResult = "";
        private string _status = "Готов к работе";
        private bool _isRunning;

        
        private readonly object _lockObject = new object();

        
        private ManualResetEvent? _sortCompletedEvent;

       
        private int[]? _sharedArray;

     
        private readonly Dispatcher _dispatcher;

        public string InputArray
        {
            get => _inputArray;
            set { _inputArray = value; OnPropertyChanged(); }
        }

        public string SearchNumber
        {
            get => _searchNumber;
            set { _searchNumber = value; OnPropertyChanged(); }
        }

        public string SortedArray
        {
            get => _sortedArray;
            set { _sortedArray = value; OnPropertyChanged(); }
        }

        public string SearchResult
        {
            get => _searchResult;
            set { _searchResult = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                OnPropertyChanged();
                ((RelayCommand)StartCommand).RaiseCanExecuteChanged();
            }
        }

        public ICommand StartCommand { get; }

        public MainViewModel()
        {
            
            _dispatcher = Dispatcher.CurrentDispatcher;

            StartCommand = new RelayCommand(
                () => StartThreads(),
                () => !IsRunning
            );
        }

       
        private void UpdateUI(Action action)
        {
            _dispatcher.Invoke(action);
        }

        private void StartThreads()
        {
            IsRunning = true;
            Status = "Потоки запущены...";
            SortedArray = "";
            SearchResult = "";

          
            _sortCompletedEvent = new ManualResetEvent(false);

    
            int[] inputData = InputArray.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                          .Select(int.Parse)
                                          .ToArray();

            int targetNumber = int.Parse(SearchNumber);

         
            Thread sortThread = new Thread(() => SortThreadWorker(inputData));
            sortThread.Name = "SortThread";
            sortThread.IsBackground = true;

         
            Thread searchThread = new Thread(() => SearchThreadWorker(targetNumber));
            searchThread.Name = "SearchThread";
            searchThread.IsBackground = true;

      
            sortThread.Start();
            searchThread.Start();
        }

    
        private void SortThreadWorker(int[] data)
        {
            UpdateUI(() => Status = "[Поток 1] Начинаю сортировку...");

           
            lock (_lockObject)
            {
                _sharedArray = new int[data.Length];
                Array.Copy(data, _sharedArray, data.Length);
            }

      
            Thread.Sleep(1000);

          
            lock (_lockObject)
            {
                if (_sharedArray != null)
                {
                    Array.Sort(_sharedArray);
                    string sorted = string.Join(" ", _sharedArray);
                    UpdateUI(() => SortedArray = sorted);
                }
            }

            UpdateUI(() => Status = "[Поток 1] Сортировка завершена!");

            
            _sortCompletedEvent?.Set();
        }

        
        private void SearchThreadWorker(int targetNumber)
        {
            UpdateUI(() => Status = "[Поток 2] Ожидаю завершения сортировки...");

            
            _sortCompletedEvent?.WaitOne();

            UpdateUI(() => Status = "[Поток 2] Сортировка завершена, начинаю поиск...");

            bool found = false;

          
            lock (_lockObject)
            {
                if (_sharedArray != null)
                {
                    
                    found = Array.BinarySearch(_sharedArray, targetNumber) >= 0;
                }
            }

    
            if (found)
            {
                UpdateUI(() => SearchResult = $"Число {targetNumber} НАЙДЕНО в массиве!");
            }
            else
            {
                UpdateUI(() => SearchResult = $"Число {targetNumber} НЕ найдено в массиве.");
            }

            UpdateUI(() => Status = "[Поток 2] Поиск завершён. Все потоки отработали.");
            UpdateUI(() => IsRunning = false);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
