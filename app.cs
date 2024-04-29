////API////
**AppDbContext.cs
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Employee> Employees { get; set; }
}

public class Employee
{
    [Key]
    public string EmployeeId { get; set; }
    public string EmployeeName { get; set; }
    public DateTime Joindate { get; set; }
    public int Salary { get; set; }
    public bool IsActive { get; set; }
    public string ImageUrl { get; set; }
}

**appsettings
"ConnectionStrings": { "Con": "server=(localdb)\\MSSQLLocalDB; database=App1DB; Trusted_Connection=true; TrustServerCertificate=true" }

**Program
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(op => op.UseSqlServer(builder.Configuration.GetConnectionString("Con")));

app.MapGet("api/employees", ([FromServices] AppDbContext db) => db.Employees).WithName("GetEmployees").Produces<Employee[]>(StatusCodes.Status200OK);

app.MapGet("api/employees/{id}",
    ([FromRoute] string id, [FromServices] AppDbContext db) => db.Employees.FirstOrDefault(e => e.EmployeeId.ToString() == id)).WithName("GetEmployee").Produces<Employee>(StatusCodes.Status200OK);

app.MapPost("api/employees", async ([FromBody] Employee employee, [FromServices] AppDbContext db) =>
{
    try
    {
        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        return Results.Created($"api/employees/{employee.EmployeeId}", employee);
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Failed to save Employee: {ex.Message}");
    }
}).WithOpenApi().Produces<Employee>(StatusCodes.Status201Created);

app.MapPut("api/employees/{id}", async ([FromRoute] string id, [FromBody] Employee employee, [FromServices] AppDbContext db) =>
{
    Employee? foundEmployee = await db.Employees.FindAsync(id);
    if (foundEmployee == null) return Results.NotFound();
    foundEmployee.EmployeeName = employee.EmployeeName;
    foundEmployee.Joindate = employee.Joindate;
    foundEmployee.IsActive = employee.IsActive;
    foundEmployee.Salary = employee.Salary;
    foundEmployee.ImageUrl = employee.ImageUrl;
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithOpenApi()
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status204NoContent);
app.MapDelete("api/employees/{id}", async ([FromRoute] string id, [FromServices] AppDbContext db) =>
{
    try
    {
        var employee = await db.Employees.FindAsync(id);
        if (employee == null)
        {
            return Results.NotFound();
        }
        db.Employees.Remove(employee);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Failed to delete employee: {ex.Message}");
    }
}).WithOpenApi().Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status204NoContent);

app.Run();

////APP////
//**Model
**Base64ToImageConverter.cs
    public class Base64ToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string base64String)
            {
                try
                {
                    byte[] imageBytes = System.Convert.FromBase64String(base64String);
                    return ImageSource.FromStream(() => new System.IO.MemoryStream(imageBytes));
                }
                catch (Exception)
                {

                }
            }


            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

**ImageFile.cs
    public class ImageFile
    {
        public string ByteBase64 { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
    }

**FileUpload.cs
    public class FileUpload
    {
        public async Task<Stream> FileResultToStream(FileResult result)
        {
            if (result == null) return null;
            return await result.OpenReadAsync();
        }
        public Stream ByteArrayToStream(byte[] bytes)
        {
            return new MemoryStream(bytes);
        }
        public string ByteBase64ToString(byte[] bytes)
        {
            return Convert.ToBase64String(bytes);
        }
        public byte[] StringToByteBase64(string text)
        {
            return Convert.FromBase64String(text);
        }
        public async Task<ImageFile> Upload(FileResult fileResult)
        {
            byte[] bytes;
            try
            {
                using (var ms = new MemoryStream())
                {
                    var stream = await FileResultToStream(fileResult);
                    stream.CopyTo(ms);
                    bytes = ms.ToArray();
                }
                return new ImageFile
                {
                    ByteBase64 = ByteBase64ToString(bytes),
                    ContentType = fileResult.ContentType,
                    FileName = fileResult.FileName
                };
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
                return null;
            }
        }
    }
//

**EmployeeViewModel.cs
    public class EmployeeViewModel : INotifyPropertyChanged
    {
        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private string employeeId;
        private string employeeName;
        private DateTime joinDate;
        private decimal salary;
        private bool isActive;
        private string imageUrl;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string EmployeeId
        {
            get => employeeId;
            set
            {
                employeeId = value;
                NotifyPropertyChanged();
            }
        }
        public string EmployeeName
        {
            get => employeeName;
            set
            {
                employeeName = value;
                NotifyPropertyChanged();
            }
        }
        public DateTime JoinDate
        {
            get => joinDate;
            set
            {
                joinDate = value;
                NotifyPropertyChanged();
            }
        }
        public decimal Salary
        {
            get => salary;
            set
            {
                salary = value;
                NotifyPropertyChanged();
            }
        }
        public bool IsActive
        {
            get => isActive;
            set
            {
                isActive = value;
                NotifyPropertyChanged();
            }
        }
        public string ImageUrl
        {
            get => imageUrl;
            set
            {
                imageUrl = value;
                NotifyPropertyChanged();
            }
        }
    }

**EmployeeListViewModel.cs
    public class EmployeeListViewModel : ObservableCollection<EmployeeViewModel>
    {
        public void AddSampleDetailifEmpty()
        {
            if (Count == 0)
            {
                Add(new EmployeeViewModel
                {
                    EmployeeId = "001",
                    EmployeeName = "Employee 1",
                    JoinDate = new DateTime(2020, 5, 15),
                    Salary = 100000,
                    IsActive = true,
                    ImageUrl = "",
                });
            }
        }
    }

**EmployeesPage.xaml
    <VerticalStackLayout Spacing="15" Padding="20">
        <HorizontalStackLayout Spacing="10">
            <Label Text="Employees" FontSize="Title"></Label>
            <Button x:Name="btnAdd" Text="Add Employee" FontSize="Title" HorizontalOptions="End" Clicked="btnAdd_Clicked" />
        </HorizontalStackLayout>
        <Label x:Name="infoLabel"/>
        <Label x:Name="ErrorLabel" IsVisible="False"/>
        <ListView ItemsSource="{Binding .}" VerticalOptions="Start" HorizontalOptions="Start" IsPullToRefreshEnabled="True" ItemTapped="Employee_ItemTapped" Refreshing="Employee_Refreshing">
            <ListView.ItemTemplate>
                <DataTemplate>
                    <TextCell Text="{Binding EmployeeName}" Detail="{Binding Joindate}" TextColor="Blue" DetailColor="DimGray">
                        <TextCell.ContextActions>
                            <MenuItem Text="Delete" IsDestructive="True" Clicked="EmployeeMenuItem_Clicked"/>
                        </TextCell.ContextActions>
                    </TextCell>
                </DataTemplate>
            </ListView.ItemTemplate>

        </ListView>
    </VerticalStackLayout>

**EmployeesPage.xaml.cs
    public partial class EmployeesPage : ContentPage
    {
        public EmployeesPage()
        {
            InitializeComponent();
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            EmployeeListViewModel viewModel = new();
            viewModel.Clear();
            LoadEmployees();
        }

        private void LoadEmployees()
        {
            EmployeeListViewModel viewModel = new();
            try
            {
                HttpClient client = new()
                {
                    BaseAddress = new Uri(DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5196" : "http://localhost:5196")
                };
                infoLabel.Text = $"BaseAddress:{client.BaseAddress}";
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response = client.GetAsync("api/employees").Result;
                response.EnsureSuccessStatusCode();

                IEnumerable<EmployeeViewModel> employeesFromService = response.Content.ReadFromJsonAsync<IEnumerable<EmployeeViewModel>>().Result;
                foreach (EmployeeViewModel e in employeesFromService.OrderBy(emp => emp.EmployeeName))
                {
                    viewModel.Add(e);
                }
                infoLabel.Text += $"\n{viewModel.Count} Employees Loaded";
            }
            catch (Exception ex)
            {
                ErrorLabel.Text = ex.Message + "\n Using sample data.";
                ErrorLabel.IsVisible = true;
            }
            BindingContext = viewModel;
        }

        private async void btnAdd_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new EmployeeDetailPage(BindingContext as EmployeeListViewModel));
        }

        private async void Employee_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item is not EmployeeViewModel c) return;
            await Navigation.PushAsync(new EmployeeDetailPage(BindingContext as EmployeeListViewModel, c));
        }

        private async void Employee_Refreshing(object sender, EventArgs e)
        {
            if (sender is not ListView listView) return;
            listView.IsRefreshing = true;
            await Task.Delay(1500);
            listView.IsRefreshing = false;
        }

        private async void EmployeeMenuItem_Clicked(object sender, EventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem.BindingContext is not EmployeeViewModel emp) return;
            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5196" : "http://localhost:5196");
                HttpResponseMessage response = await client.DeleteAsync($"api/employees/{emp.EmployeeId}");
                if (response.IsSuccessStatusCode)
                {
                    (BindingContext as EmployeeListViewModel)?.Remove(emp);
                }
                else
                {
                    await DisplayAlert("Error", "Falied to delete", "OK");
                }
            }
            catch (Exception ex)
            {

                await DisplayAlert("Error", $"Error Occured: {ex.Message}", "OK");
            }
        }
    }

**EmployeeDetailPage.xaml
     <ScrollView>
        <VerticalStackLayout Spacing="10" Padding="10">
            <Frame>
                <Entry x:Name="EmployeeId" Text="{Binding EmployeeId, Mode=TwoWay}" Placeholder="Enter EmployeeId"/>
            </Frame>
            <Frame>
                <Entry x:Name="EmployeeName" Text="{Binding EmployeeName, Mode=TwoWay}" Placeholder="Enter Name"/>
            </Frame>
            <Frame>
                <DatePicker x:Name="JoinDate" Date="{Binding JoinDate, Mode=TwoWay}"/>
            </Frame>
            <Frame>
                <Entry Placeholder="Enter salary" x:Name="Salary" Text="{Binding Salary, Mode=TwoWay}"/>
            </Frame>
            <Frame>
                <Switch x:Name="IsActive" IsToggled="{Binding IsActive, Mode=TwoWay}"/>
            </Frame>

            <Frame>
                <!--<Image Source="{Binding ImageUrl, Converter={StaticResource Base64ToImageConverter}}" HeightRequest="100" x:Name="Upload_Image" HorizontalOptions="Center" />-->

            </Frame>
            <StackLayout>
                <Image Source="{Binding ImageUrl, Converter={StaticResource Base64ToImageConverter}}" HeightRequest="100" x:Name="Upload_Image" HorizontalOptions="Center" />
                <Button x:Name="btnUpload" Text="Upload" HorizontalOptions="End" Clicked="btnUpload_Clicked"/>
            </StackLayout>
            <Frame>
                <StackLayout>
                    <Button x:Name="btnAddEmployee" Text="Add Employee" HorizontalOptions="End" Clicked="btnAddEmployee_Clicked"/>
                    <Button x:Name="btnUpdateEmployee" Text="Update Employee" HorizontalOptions="End" Clicked="btnUpdateEmployee_Clicked"/>
                </StackLayout>
            </Frame>
        </VerticalStackLayout>
    </ScrollView>

**EmployeeDetailPage.xaml.cs
    public partial class EmployeeDetailPage : ContentPage
    {
        private EmployeeListViewModel employees;
        FileUpload obj = new();
        FileUpload imageUpload { get; set; }
        private string SelectedImageBase64 { get; set; }
        public EmployeeDetailPage()
        {
            InitializeComponent();
        }
        public EmployeeDetailPage(EmployeeListViewModel employees, EmployeeViewModel employee)
        {
            InitializeComponent();
            this.employees = employees;
            BindingContext = employee;
            Title = "Edit Employee";
            btnAddEmployee.IsVisible = false;
            btnUpdateEmployee.IsVisible = true;

            (BindingContext as EmployeeViewModel)?.NotifyPropertyChanged("ImageUrl");

            imageUpload = new FileUpload();

        }
        public EmployeeDetailPage(EmployeeListViewModel employees)
        {
            InitializeComponent();
            this.employees = employees;
            BindingContext = new EmployeeViewModel();
            Title = "Add Employee";
        }
        private async void btnUpload_Clicked(object sender, EventArgs e)
        {
            var img = await MediaPicker.PickPhotoAsync();
            var imageFile = await obj.Upload(img);
            SelectedImageBase64 = imageFile?.ByteBase64;
            Upload_Image.Source = ImageSource.FromStream(() =>
            obj.ByteArrayToStream(obj.StringToByteBase64(imageFile.ByteBase64))

            );
        }

        private async void btnAddEmployee_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (BindingContext is EmployeeViewModel employeeViewModel)
                {
                    JsonSerializerOptions _serializerOptions;
                    _serializerOptions = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = true
                    };
                    employeeViewModel.ImageUrl = SelectedImageBase64;

                    HttpClient client = new HttpClient();
                    client.BaseAddress = new Uri(DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5196" : "http://localhost:5196");

                    string json = System.Text.Json.JsonSerializer.Serialize<EmployeeViewModel>(employeeViewModel, _serializerOptions);
                    StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                    client.DefaultRequestHeaders
                     .Accept
                     .Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    HttpResponseMessage response = await client.PostAsync("api/employees", content);




                    if (response.IsSuccessStatusCode)
                    {
                        await DisplayAlert("Success", "Employee saved successfully.", "OK");
                        await Navigation.PopAsync(animated: true);
                    }
                    else
                    {
                        await DisplayAlert("Error", "Failed to save employee.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        private async void btnUpdateEmployee_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (BindingContext is EmployeeViewModel employeeViewModel)
                {
                    JsonSerializerOptions _serializerOptions;
                    _serializerOptions = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = true
                    };
                    employeeViewModel.ImageUrl = SelectedImageBase64;

                    HttpClient client = new HttpClient();
                    client.BaseAddress = new Uri(DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5196" : "http://localhost:5196");


                    string json = System.Text.Json.JsonSerializer.Serialize<EmployeeViewModel>(employeeViewModel, _serializerOptions);
                    StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                    client.DefaultRequestHeaders
                     .Accept
                     .Add(new MediaTypeWithQualityHeaderValue("application/json"));



                    HttpResponseMessage response = await client.PutAsync($"api/employees/{employeeViewModel.EmployeeId}", content);

                    if (response.IsSuccessStatusCode)
                    {
                        await DisplayAlert("Success", "Employee updated successfully.", "OK");
                        await Navigation.PopAsync(animated: true);
                    }
                    else
                    {
                        await DisplayAlert("Error", "Failed to update employee.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }
    }

**AppShell.xaml
        <ShellContent
        Title="Employee"
        ContentTemplate="{DataTemplate local:EmployeesPage}"
        Route="EmployeesPage" />

//**Resources/xml
**network_security_config.xml [AndroidResource -Enable]
<network-security-config>
	<base-config cleartextTrafficPermitted="true" />
	<domain-config cleartextTrafficPermitted="true">
		<domain includeSubdomains="true">10.0.2.2</domain>
	</domain-config>
</network-security-config>

**AndroidManifest.xml
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
	<application
		android:networkSecurityConfig="@xml/network_security_config"
		android:allowBackup="true" android:icon="@mipmap/appicon" android:roundIcon="@mipmap/appicon_round" android:supportsRtl="true"></application>
	<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
	<uses-permission android:name="android.permission.INTERNET" />
</manifest>