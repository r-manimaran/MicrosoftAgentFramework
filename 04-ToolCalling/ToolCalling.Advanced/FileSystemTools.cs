using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToolCalling.Advanced;

public class FileSystemTools
{
    public string RootFolder { get; set; }

    public FileSystemTools()
    {
        RootFolder = @"C:\Maran\FunctionCallingExample";
        if (!Directory.Exists(RootFolder))
        {
            Directory.CreateDirectory(RootFolder);
        }
    }

    public string GetRootFolder()
    {
        return RootFolder;
    }

    public void CreateFolder(string folderPath)
    {
        Guard(folderPath);
        Directory.CreateDirectory(folderPath);
    }
    public void CreateFile(string filePath, string fileContent)
    {
        Guard(filePath);
        File.WriteAllText(filePath, fileContent);
    }
    public string GetFileContent(string filePath)
    {
        Guard(filePath);
        return File.ReadAllText(filePath);
    }
    public void MoveFile(string sourceFilePath, string destinationFilePath)
    {
        Guard(sourceFilePath);
        Guard(destinationFilePath);
        File.Move(sourceFilePath, destinationFilePath);
    }
    public void MoveFolder(string sourceFolderPath, string destinationFolderPath)
    {
        Guard(sourceFolderPath);
        Directory.Move(sourceFolderPath, destinationFolderPath);
    }

    public string[] GetFiles(string folderPath)
    {
        Guard(folderPath);
        return Directory.GetFiles(folderPath);
    }
    public string[] GetFolders(string folderPath)
    {
        Guard(folderPath);
        return Directory.GetDirectories(folderPath);
    }
    public void DeleteFile(string filePath)
    {
        Guard(filePath);
        File.Delete(filePath);
    }
    public void DeleteFolder(string folderPath)
    {
        if(folderPath == RootFolder)
        {
            throw new InvalidOperationException("Cannot delete the root folder.");
        }
        Guard(folderPath);
        Directory.Delete(folderPath);
    }
    public void Guard(string folderPath)
    {
        if (!folderPath.StartsWith(RootFolder, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Operation outside the root folder is not allowed.");
        }
    }

}
