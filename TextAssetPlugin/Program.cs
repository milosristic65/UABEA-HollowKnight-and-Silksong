using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UABEAvalonia;
using UABEAvalonia.Plugins;

namespace TextAssetPlugin
{
    public static class TextAssetHelper
    {
        public static string GetUContainerExtension(AssetContainer item)
        {
            string ucont = item.Container;
            if (Path.GetFileName(ucont) != Path.GetFileNameWithoutExtension(ucont))
            {
                return Path.GetExtension(ucont);
            }

            return string.Empty;
        }
    }

    public class ImportTextAssetOption : UABEAPluginOption
    {
        public bool SelectionValidForPlugin(AssetsManager am, UABEAPluginAction action, List<AssetContainer> selection, out string name)
        {
            name = "Import .txt";

            if (action != UABEAPluginAction.Import)
                return false;

            foreach (AssetContainer cont in selection)
            {
                if (cont.ClassId != (int)AssetClassID.TextAsset)
                    return false;
            }
            return true;
        }

        public async Task<bool> ExecutePlugin(Window win, AssetWorkspace workspace, List<AssetContainer> selection)
        {
            if (selection.Count > 1)
                return await BatchImport(win, workspace, selection);
            else
                return await SingleImport(win, workspace, selection);
        }

        public async Task<bool> BatchImport(Window win, AssetWorkspace workspace, List<AssetContainer> selection)
        {
            var selectedFolders = await win.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
            {
                Title = "Select import directory"
            });

            string[] selectedFolderPaths = FileDialogUtils.GetOpenFolderDialogFiles(selectedFolders);
            if (selectedFolderPaths.Length == 0)
                return false;

            string dir = selectedFolderPaths[0];

            List<string> extensions = new List<string>() { "*" };
            ImportBatch dialog = new ImportBatch(workspace, selection, dir, extensions);
            List<ImportBatchInfo> batchInfos = await dialog.ShowDialog<List<ImportBatchInfo>>(win);
            foreach (ImportBatchInfo batchInfo in batchInfos)
            {
                AssetContainer cont = batchInfo.cont;

                AssetTypeValueField baseField = workspace.GetBaseField(cont);

                string file = batchInfo.importFile;

                byte[] byteData = File.ReadAllBytes(file);
                baseField["m_Script"].AsByteArray = byteData;

                byte[] savedAsset = baseField.WriteToByteArray();

                var replacer = new AssetsReplacerFromMemory(
                    cont.PathId, cont.ClassId, cont.MonoId, savedAsset);

                workspace.AddReplacer(cont.FileInstance, replacer, new MemoryStream(savedAsset));
            }
            return true;
        }
        public async Task<bool> SingleImport(Window win, AssetWorkspace workspace, List<AssetContainer> selection)
        {
            AssetContainer cont = selection[0];

            AssetTypeValueField baseField = workspace.GetBaseField(cont);

            var filters = new List<FilePickerFileType>()
            {
                new FilePickerFileType("Text files (*.txt)") { Patterns = new List<string>() { "*.txt" } },
                new FilePickerFileType("All types (*.*)") { Patterns = new List<string>() { "*" } }
            };

            string ucontExt = TextAssetHelper.GetUContainerExtension(cont);
            if (ucontExt != string.Empty)
            {
                string ucontExtNoDot = ucontExt[1..];
                string displayName = $"{ucontExtNoDot} files (*{ucontExt})";
                List<string> patterns = new List<string>() { "*" + ucontExt };
                filters.Insert(0, new FilePickerFileType(displayName) { Patterns = patterns });
            }

            var selectedFiles = await win.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                Title = "Open text file",
                FileTypeFilter = filters
            });

            string[] selectedFilePaths = FileDialogUtils.GetOpenFileDialogFiles(selectedFiles);
            if (selectedFilePaths.Length == 0)
                return false;

            string file = selectedFilePaths[0];

            byte[] byteData = File.ReadAllBytes(file);
            baseField["m_Script"].AsByteArray = byteData;

            byte[] savedAsset = baseField.WriteToByteArray();

            var replacer = new AssetsReplacerFromMemory(
                cont.PathId, cont.ClassId, cont.MonoId, savedAsset);

            workspace.AddReplacer(cont.FileInstance, replacer, new MemoryStream(savedAsset));
            return true;
        }
    }

    public class ExportTextAssetOption : UABEAPluginOption
    {
        public bool SelectionValidForPlugin(AssetsManager am, UABEAPluginAction action, List<AssetContainer> selection, out string name)
        {
            name = "Export .txt";

            if (action != UABEAPluginAction.Export)
                return false;

            foreach (AssetContainer cont in selection)
            {
                if (cont.ClassId != (int)AssetClassID.TextAsset)
                    return false;
            }
            return true;
        }

        public async Task<bool> ExecutePlugin(Window win, AssetWorkspace workspace, List<AssetContainer> selection)
        {
            if (selection.Count > 1)
                return await BatchExport(win, workspace, selection);
            else
                return await SingleExport(win, workspace, selection);
        }

        public async Task<bool> BatchExport(Window win, AssetWorkspace workspace, List<AssetContainer> selection)
        {
            var selectedFolders = await win.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
            {
                Title = "Select export directory"
            });

            string[] selectedFolderPaths = FileDialogUtils.GetOpenFolderDialogFiles(selectedFolders);
            if (selectedFolderPaths.Length == 0)
                return false;

            string dir = selectedFolderPaths[0];

            foreach (AssetContainer cont in selection)
            {
                AssetTypeValueField baseField = workspace.GetBaseField(cont);

                string name = baseField["m_Name"].AsString;
                byte[] byteData = baseField["m_Script"].AsByteArray;

                name = PathUtils.ReplaceInvalidPathChars(name);

                string extension = ".txt";
                string ucontExt = TextAssetHelper.GetUContainerExtension(cont);
                if (ucontExt != string.Empty)
                {
                    extension = ucontExt;
                }

                string file = Path.Combine(dir, $"{name}-{Path.GetFileName(cont.FileInstance.path)}-{cont.PathId}{extension}");

                File.WriteAllBytes(file, byteData);
            }
            return true;
        }
        public async Task<bool> SingleExport(Window win, AssetWorkspace workspace, List<AssetContainer> selection)
        {
            AssetContainer cont = selection[0];

            AssetTypeValueField baseField = workspace.GetBaseField(cont);
            string name = baseField["m_Name"].AsString;
            name = PathUtils.ReplaceInvalidPathChars(name);

            var filters = new List<FilePickerFileType>()
            {
                new FilePickerFileType("Text files (*.txt)") { Patterns = new List<string>() { "*.txt" } },
                new FilePickerFileType("All types (*.*)") { Patterns = new List<string>() { "*" } }
            };

            string defaultExtension = "txt";

            string ucontExt = TextAssetHelper.GetUContainerExtension(cont);
            if (ucontExt != string.Empty)
            {
                string ucontExtNoDot = ucontExt[1..];
                string displayName = $"{ucontExtNoDot} files (*{ucontExt})";
                List<string> patterns = new List<string>() { "*" + ucontExt };
                filters.Insert(0, new FilePickerFileType(displayName) { Patterns = patterns });
                defaultExtension = ucontExtNoDot;
            }

            var selectedFile = await win.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
            {
                Title = "Save text file",
                FileTypeChoices = filters,
                DefaultExtension = defaultExtension,
                SuggestedFileName = $"{name}-{Path.GetFileName(cont.FileInstance.path)}-{cont.PathId}"
            });

            string selectedFilePath = FileDialogUtils.GetSaveFileDialogFile(selectedFile);
            if (selectedFilePath == null)
                return false;

            byte[] byteData = baseField["m_Script"].AsByteArray;
            File.WriteAllBytes(selectedFilePath, byteData);

            return true;
        }
    }

    public class ImportTextAssetOptionHK : UABEAPluginOption
    {
        static byte[] key = Encoding.UTF8.GetBytes("UKu52ePUBwetZ9wNX88o54dnfKRu0T1l");

        public bool SelectionValidForPlugin(AssetsManager am, UABEAPluginAction action, List<AssetContainer> selection, out string name)
        {
            name = "Import .txt (Hollow Knight)";

            if (action != UABEAPluginAction.Import)
                return false;

            foreach (AssetContainer cont in selection)
            {
                if (cont.ClassId != (int)AssetClassID.TextAsset)
                    return false;
            }
            return true;
        }

        public async Task<bool> ExecutePlugin(Window win, AssetWorkspace workspace, List<AssetContainer> selection)
        {
            if (selection.Count > 1)
                return await BatchImport(win, workspace, selection);
            else
                return await SingleImport(win, workspace, selection);
        }

        public async Task<bool> BatchImport(Window win, AssetWorkspace workspace, List<AssetContainer> selection)
        {
            var selectedFolders = await win.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
            {
                Title = "Select import directory"
            });

            string[] selectedFolderPaths = FileDialogUtils.GetOpenFolderDialogFiles(selectedFolders);
            if (selectedFolderPaths.Length == 0)
                return false;

            string dir = selectedFolderPaths[0];

            List<string> extensions = new List<string>() { "*" };
            ImportBatch dialog = new ImportBatch(workspace, selection, dir, extensions);
            List<ImportBatchInfo> batchInfos = await dialog.ShowDialog<List<ImportBatchInfo>>(win);
            foreach (ImportBatchInfo batchInfo in batchInfos)
            {
                AssetContainer cont = batchInfo.cont;

                AssetTypeValueField baseField = workspace.GetBaseField(cont);

                string file = batchInfo.importFile;

                string plaintext = File.ReadAllText(file, Encoding.UTF8);
                byte[] encryptedBytes = EncryptHK(plaintext);
                string base64 = Convert.ToBase64String(encryptedBytes);
                byte[] base64Bytes = Encoding.UTF8.GetBytes(base64);
                baseField["m_Script"].AsByteArray = base64Bytes;

                byte[] savedAsset = baseField.WriteToByteArray();

                var replacer = new AssetsReplacerFromMemory(
                    cont.PathId, cont.ClassId, cont.MonoId, savedAsset);

                workspace.AddReplacer(cont.FileInstance, replacer, new MemoryStream(savedAsset));
            }
            return true;
        }
        public async Task<bool> SingleImport(Window win, AssetWorkspace workspace, List<AssetContainer> selection)
        {
            AssetContainer cont = selection[0];

            AssetTypeValueField baseField = workspace.GetBaseField(cont);

            var filters = new List<FilePickerFileType>()
            {
                new FilePickerFileType("Text files (*.txt)") { Patterns = new List<string>() { "*.txt" } },
                new FilePickerFileType("All types (*.*)") { Patterns = new List<string>() { "*" } }
            };

            string ucontExt = TextAssetHelper.GetUContainerExtension(cont);
            if (ucontExt != string.Empty)
            {
                string ucontExtNoDot = ucontExt[1..];
                string displayName = $"{ucontExtNoDot} files (*{ucontExt})";
                List<string> patterns = new List<string>() { "*" + ucontExt };
                filters.Insert(0, new FilePickerFileType(displayName) { Patterns = patterns });
            }

            var selectedFiles = await win.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                Title = "Open text file",
                FileTypeFilter = filters
            });

            string[] selectedFilePaths = FileDialogUtils.GetOpenFileDialogFiles(selectedFiles);
            if (selectedFilePaths.Length == 0)
                return false;

            string file = selectedFilePaths[0];

            string plaintext = File.ReadAllText(file, Encoding.UTF8);
            byte[] encryptedBytes = EncryptHK(plaintext);
            string base64 = Convert.ToBase64String(encryptedBytes);
            byte[] base64Bytes = Encoding.UTF8.GetBytes(base64);
            baseField["m_Script"].AsByteArray = base64Bytes;

            byte[] savedAsset = baseField.WriteToByteArray();

            var replacer = new AssetsReplacerFromMemory(
                cont.PathId, cont.ClassId, cont.MonoId, savedAsset);

            workspace.AddReplacer(cont.FileInstance, replacer, new MemoryStream(savedAsset));
            return true;
        }

        private static byte[] EncryptHK(string plaintext)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plaintext);
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform encryptor = aes.CreateEncryptor();
                return encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            }
        }
    }

    public class ExportTextAssetOptionHK : UABEAPluginOption
    {
        static byte[] key = Encoding.UTF8.GetBytes("UKu52ePUBwetZ9wNX88o54dnfKRu0T1l");

        public bool SelectionValidForPlugin(AssetsManager am, UABEAPluginAction action, List<AssetContainer> selection, out string name)
        {
            name = "Export .txt (Hollow Knight)";

            if (action != UABEAPluginAction.Export)
                return false;

            foreach (AssetContainer cont in selection)
            {
                if (cont.ClassId != (int)AssetClassID.TextAsset)
                    return false;
            }
            return true;
        }

        public async Task<bool> ExecutePlugin(Window win, AssetWorkspace workspace, List<AssetContainer> selection)
        {
            if (selection.Count > 1)
                return await BatchExport(win, workspace, selection);
            else
                return await SingleExport(win, workspace, selection);
        }

        public async Task<bool> BatchExport(Window win, AssetWorkspace workspace, List<AssetContainer> selection)
        {
            var selectedFolders = await win.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
            {
                Title = "Select export directory"
            });

            string[] selectedFolderPaths = FileDialogUtils.GetOpenFolderDialogFiles(selectedFolders);
            if (selectedFolderPaths.Length == 0)
                return false;

            string dir = selectedFolderPaths[0];

            foreach (AssetContainer cont in selection)
            {
                AssetTypeValueField baseField = workspace.GetBaseField(cont);
                string name = PathUtils.ReplaceInvalidPathChars(baseField["m_Name"].AsString);

                byte[] byteData = baseField["m_Script"].AsByteArray;
                string base64 = Encoding.UTF8.GetString(byteData);
                byteData = Convert.FromBase64String(base64);
                string decryptedText = DecryptHK(byteData);

                string extension = ".txt";
                string ucontExt = TextAssetHelper.GetUContainerExtension(cont);
                if (ucontExt != string.Empty)
                    extension = ucontExt;

                string file = Path.Combine(dir, $"{name}-{Path.GetFileName(cont.FileInstance.path)}-{cont.PathId}{extension}");
                File.WriteAllText(file, decryptedText, Encoding.UTF8);
            }

            return true;
        }

        public async Task<bool> SingleExport(Window win, AssetWorkspace workspace, List<AssetContainer> selection)
        {
            AssetContainer cont = selection[0];
            AssetTypeValueField baseField = workspace.GetBaseField(cont);
            string name = PathUtils.ReplaceInvalidPathChars(baseField["m_Name"].AsString);

            var filters = new List<FilePickerFileType>()
        {
            new FilePickerFileType("Text files (*.txt)") { Patterns = new List<string>() { "*.txt" } },
            new FilePickerFileType("All types (*.*)") { Patterns = new List<string>() { "*" } }
        };

            string defaultExtension = "txt";

            string ucontExt = TextAssetHelper.GetUContainerExtension(cont);
            if (ucontExt != string.Empty)
            {
                string ucontExtNoDot = ucontExt[1..];
                filters.Insert(0, new FilePickerFileType($"{ucontExtNoDot} files (*{ucontExt})") { Patterns = new List<string>() { "*" + ucontExt } });
                defaultExtension = ucontExtNoDot;
            }

            var selectedFile = await win.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
            {
                Title = "Save text file",
                FileTypeChoices = filters,
                DefaultExtension = defaultExtension,
                SuggestedFileName = $"{name}-{Path.GetFileName(cont.FileInstance.path)}-{cont.PathId}"
            });

            string selectedFilePath = FileDialogUtils.GetSaveFileDialogFile(selectedFile);
            if (selectedFilePath == null)
                return false;

            byte[] byteData = baseField["m_Script"].AsByteArray;
            string base64 = Encoding.UTF8.GetString(byteData);
            byteData = Convert.FromBase64String(base64);

            string decryptedText = DecryptHK(byteData);
            File.WriteAllText(selectedFilePath, decryptedText, Encoding.UTF8);

            return true;
        }

        private static string DecryptHK(byte[] encryptedBytes)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform decryptor = aes.CreateDecryptor();
                byte[] decrypted = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                return Encoding.UTF8.GetString(decrypted);
            }
        }
    }

    public class TextAssetPlugin : UABEAPlugin
    {
        public PluginInfo Init()
        {
            PluginInfo info = new PluginInfo();
            info.name = "TextAsset Import/Export";

            info.options = new List<UABEAPluginOption>();
            info.options.Add(new ImportTextAssetOption());
            info.options.Add(new ExportTextAssetOption());
            info.options.Add(new ImportTextAssetOptionHK());
            info.options.Add(new ExportTextAssetOptionHK());
            return info;
        }
    }
}
