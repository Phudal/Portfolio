using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Security.Cryptography;
using System;
using System.Text;
using System.IO;
using UnityEditor;

public class AESCrypto
{
    private static AESCrypto instance;
    public static AESCrypto Instance
    {
        get
        {
            if (instance == null)
                instance = new AESCrypto();

            return instance;
        }
    }

    private string key = "";   

    // AES 암호화
    public string AESEncrypt(string input)
    {
        LoadDataAsset();

        try
        {
            RijndaelManaged aes = new RijndaelManaged();

            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.Zeros;
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };        

            var encrypt = aes.CreateEncryptor(aes.Key, aes.IV);

            byte[] buf = null;
            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, encrypt, CryptoStreamMode.Write))
                {
                    byte[] xXml = Encoding.UTF8.GetBytes(input);
                    cs.Write(xXml, 0, xXml.Length);
                }
                buf = ms.ToArray();
            }
            string Output = Convert.ToBase64String(buf);
            return Output;
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            return e.Message;
        }
    }

    // AES 복호화
    public string AESDecrypt(string input)
    {
        LoadDataAsset();

        try
        {
            RijndaelManaged aes = new RijndaelManaged();

            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.Zeros;
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            var decrypt = aes.CreateDecryptor();

            byte[] buf = null;
            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, decrypt, CryptoStreamMode.Write))
                {
                    byte[] xXml = Convert.FromBase64String(input);
                    cs.Write(xXml, 0, xXml.Length);
                }
                buf = ms.ToArray();
            }
            string Output = Encoding.UTF8.GetString(buf);           

            return Output;
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            return string.Empty;
        }
    }

    private void LoadDataAsset()
    {
        var dataAsset = Resources.Load<AESCryptoData>("AESCryptoData");
        this.key = dataAsset.key;
    }
}