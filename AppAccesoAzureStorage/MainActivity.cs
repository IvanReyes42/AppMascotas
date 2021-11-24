using Android.App;
using Android.OS;
using Android.Runtime;
using AndroidX.AppCompat.App;
using Android.Widget;
using Plugin.Media;
using System;
using System.IO;
using Plugin.CurrentActivity;
using Android.Graphics;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace AppAccesoAzureStorage
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        string Archivo;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            SupportActionBar.Hide(); //Borrar la bara de arriba
            CrossCurrentActivity.Current.Init(this, savedInstanceState);
            var Imagen = FindViewById<ImageView>(Resource.Id.imagen);
            var btnAlmacenar = FindViewById<Button>(Resource.Id.btnAlmacenar);
            var txtNombre = FindViewById<EditText>(Resource.Id.txtNombre);
            var txtEspecie = FindViewById<EditText>(Resource.Id.txtEspecie);
            var txtEdad = FindViewById<EditText>(Resource.Id.txtEdad);
            var cmbEstado = FindViewById<Spinner>(Resource.Id.cmbEstado);

            var adapter = ArrayAdapter.CreateFromResource(
             this, Resource.Array.estado_array, Android.Resource.Layout.SimpleSpinnerItem);

            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            cmbEstado.Adapter = adapter;

            Imagen.Click += async delegate
            {

                await CrossMedia.Current.Initialize(); //Inicializa el acceso a la camara
                var archivo = await CrossMedia.Current.TakePhotoAsync //Por medio de tomar fotografia
                (new Plugin.Media.Abstractions.StoreCameraMediaOptions //Propiedades que tomara de la imagenes tomada por la camara
                {
                    Directory = "Animales",
                    Name = txtNombre.Text,
                    SaveToAlbum = true,
                    CompressionQuality = 40,
                    CustomPhotoSize = 40,
                    PhotoSize = Plugin.Media.Abstractions.PhotoSize.Medium,
                    DefaultCamera = Plugin.Media.Abstractions.CameraDevice.Front

                });
                if (archivo == null) //Si la imagen esta vacia ya no continua
                    return;
                Bitmap bp = BitmapFactory.DecodeStream(archivo.GetStream()); //Inicializa el mapa de bits
                //Crea la ruta de la imagen
                Archivo = System.IO.Path.Combine(System.Environment.GetFolderPath
                    (System.Environment.SpecialFolder.Personal), txtNombre.Text + ".jpg");
                //Generamos el archivo
                var stream = new FileStream(Archivo, FileMode.Create);
                //Creamos el archivo apartir del mapa de bits
                bp.Compress(Bitmap.CompressFormat.Jpeg, 40, stream);
                stream.Close();
                Imagen.SetImageBitmap(bp);
            };

            btnAlmacenar.Click += async delegate
            {
                try
                {
                    //Subir Imagen
                    //Clave de Conexion
                    var CuentadeAlmacenamiento = CloudStorageAccount.Parse
                    ("");
                    //Creamos el cliente
                    var ClienteBlob = CuentadeAlmacenamiento.CreateCloudBlobClient();

                    var Carpeta = ClienteBlob.GetContainerReference("imagenesanimales");
                    var resourceBlob = Carpeta.GetBlockBlobReference(txtNombre.Text + ".jpg");
                    resourceBlob.Properties.ContentType = "image/jpeg";

                    await resourceBlob.UploadFromFileAsync(Archivo.ToString());
                    //Toast.MakeText(this, "Imagen Almacenada en Contenerdor de Azure", ToastLength.Long).Show();
                    
                    var TablaNoSQL = CuentadeAlmacenamiento.CreateCloudTableClient();
                    var Coleccion = TablaNoSQL.GetTableReference("datosanimales");
                    await Coleccion.CreateIfNotExistsAsync();

                    var animal = new Animales("Animales", txtNombre.Text);
                    animal.Especie = txtEspecie.Text;
                    animal.Edad = int.Parse(txtEdad.Text);
                    animal.Estado = cmbEstado.SelectedItem.ToString();
                    animal.Imagen = "https://datosdanieled.blob.core.windows.net/imagenesanimales/" + txtNombre.Text + ".jpg";

                    var Store = TableOperation.Insert(animal);

                    await Coleccion.ExecuteAsync(Store);
                    Toast.MakeText(this, "Datos Almacenados Correctamente", ToastLength.Long).Show();

                }
                catch (Exception ex)
                {
                   Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
                }
            };

        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }

    public class Animales:TableEntity
    {
        public Animales(string Categoria,string Nombre)
        {
            PartitionKey = Categoria;
            RowKey = Nombre;
        }
        public string Especie { get; set; }
        public int Edad { get; set; }
        public string Estado { get; set; }
        public string Imagen { get; set; }
    }
}