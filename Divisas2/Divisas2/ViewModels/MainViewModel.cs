using Divisas2.Models;
using System.Collections.ObjectModel;
using System;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Net.Http;
using System.Reflection;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using Plugin.Connectivity;

namespace Divisas2.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        #region Eventos
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Atributos
        private bool isRunning;

        private bool isEnabled;

        private ExchangeRates exchangeRates;

        private decimal amount;

        private string sourceRate;

        private string targetRate;

        public string message { get; set; }
        #endregion

        #region Properties

        public ObservableCollection<Rate> Rates { get; set; }

        public decimal Amount
        {
            set
            {
                if (amount != value)
                {
                    amount = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Amount"));
                }
            }
            get
            {
                return amount;
            }
        }

        public string SourceRate
        {
            set
            {
                if (sourceRate != value)
                {
                    sourceRate = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SourceRate"));
                }
            }
            get
            {
                return sourceRate;
            }
        }

        public string TargetRate
        {
            set
            {
                if (targetRate != value)
                {
                    targetRate = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TargetRate"));
                }
            }
            get
            {
                return targetRate;
            }
        }

        public string Message {
            set
            {
                if (message != value)
                {
                    message = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Message"));
                }
            }
            get
            {
                return message;
            }
        }

        public bool IsRunning
        {
            set
            {
                if (isRunning != value)
                {
                    isRunning = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsRunning"));
                }
            }
            get
            {
                return isRunning;
            }
        }

        public bool IsEnabled
        {
            set
            {
                if (isEnabled != value)
                {
                    isEnabled = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsEnabled"));
                }
            }
            get
            {
                return isEnabled;
            }
        }
        #endregion

        #region Constructores
        public MainViewModel()
        {
            Rates = new ObservableCollection<Rate>();
            GetRates();
        }
        #endregion

        #region Metodos
        private async void GetRates()
        {
            IsRunning = true;
            IsEnabled = true;

            if (!CrossConnectivity.Current.IsConnected)
            {
                IsRunning = false;
                IsEnabled = true;
                await App.Current.MainPage.DisplayAlert("Error", "Por favor verifica tu conexion a Internet", "Aceptar");
                return;
            }

            var IsReachable = await CrossConnectivity.Current.IsRemoteReachable("google.com");
            if (!IsReachable)
            {
                IsRunning = false;
                IsEnabled = true;
                await App.Current.MainPage.DisplayAlert("Error", "Por favor verifica tu conexion a Internet", "Aceptar");
                return;
            }

            try
            {
                var client = new HttpClient();
                client.BaseAddress = new Uri("https://openexchangerates.org");
                var url = "/api/latest.json?app_id=f490efbcd52d48ee98fd62cf33c47b9e";
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    await App.Current.MainPage.DisplayAlert("Error", response.StatusCode.ToString(), "Aceptar");
                    IsRunning = false;
                    IsEnabled = false;
                    return;
                }

                var result = await response.Content.ReadAsStringAsync();
                exchangeRates = JsonConvert.DeserializeObject<ExchangeRates>(result);
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", ex.Message, "Aceptar");
                IsRunning = false;
                IsEnabled = false;
                return;
            }

            LoadRates();
            IsRunning = false;
            IsEnabled = true;

        }

        private void LoadRates()
        {
            Rates.Clear();
            var type = typeof(Rates);
            var properties = type.GetRuntimeFields();

            foreach (var property in properties)
            {
                var code = property.Name.Substring(1, 3);
                Rates.Add(new Rate
                {
                    Code = code,
                    TaxRate = (double)property.GetValue(exchangeRates.Rates),
                    CodeTaxRate = string.Concat(code, property.GetValue(exchangeRates.Rates)),
                });
            }
        }
        #endregion

        #region Comandos
        public ICommand ConvertMoneyCommand
        {
            get
            {
                return new RelayCommand(ConvertMoney);
            }
        }

        private async void ConvertMoney()
        {
            double sourceRateW = 0;
            double targetRateW = 0;
            try
            {
                sourceRateW = double.Parse(SourceRate.Substring(3));
                targetRateW = double.Parse(TargetRate.Substring(3));
            }
            catch (Exception)
            {
                sourceRateW = 0;
                targetRateW = 0;
            }
            

            if (Amount <= 0)
            {
                await App.Current.MainPage.DisplayAlert("Error", "Debes ingresar un valor a convertir", "Aceptar");
                return;
            }

            if (sourceRateW == 0)
            {
                await App.Current.MainPage.DisplayAlert("Error", "Debes seleccionar la moneda origen", "Aceptar");
                return;
            }

            if (targetRateW == 0)
            {
                await App.Current.MainPage.DisplayAlert("Error", "Debes seleccionar la moneda destino", "Aceptar");
                return;
            }

            decimal amountConverted = Amount / (decimal)sourceRateW * (decimal)targetRateW;

            Message = string.Format("{0:N2} = {1:N2}", Amount, amountConverted);
        }

        public ICommand TraslateCodeCommand
        {
            get
            {
                return new RelayCommand(TraslateCode);
            }
        }

        private void TraslateCode()
        {
            string target = TargetRate;
            TargetRate = SourceRate;
            SourceRate = target;
        }
    }
    #endregion
}