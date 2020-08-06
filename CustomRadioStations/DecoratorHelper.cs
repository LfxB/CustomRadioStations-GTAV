using System;
using System.Diagnostics;
using GTA;
using GTA.Native;

public static class DecoratorHelper
{
    public enum DecoratorType
    {
        Float = 1,
        Bool,
        Integer,
        Time = 5
    }

    public static void SAFE_DECOR_REGISTER(string propertyName, DecoratorType type)
    {
        if (DECOR_IS_REGISTERED_AS_TYPE(propertyName, type)) return;

        DECOR_REGISTER(propertyName, type);
    }

    public static void DECOR_REGISTER(string propertyName, DecoratorType type)
    {
        Function.Call(Hash.DECOR_REGISTER, propertyName, (int)type);
    }

    public static bool DECOR_IS_REGISTERED_AS_TYPE(string propertyName, DecoratorType type)
    {
        return Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE, propertyName, (int)type);
    }

    public static bool DECOR_EXIST_ON(Entity entity, string propertyName)
    {
        if (entity == null || !entity.Exists()) return false;

        return Function.Call<bool>(Hash.DECOR_EXIST_ON, entity, propertyName);
    }

    public static bool DECOR_GET_BOOL(Entity entity, string propertyName)
    {
        if (entity == null || !entity.Exists()) return false;

        return Function.Call<bool>(Hash.DECOR_GET_BOOL, entity, propertyName);
    }

    public static float DECOR_GET_FLOAT(Entity entity, string propertyName)
    {
        if (entity == null || !entity.Exists()) return -8008f;

        return Function.Call<float>(Hash._DECOR_GET_FLOAT, entity, propertyName);
    }

    public static int DECOR_GET_INT(Entity entity, string propertyName)
    {
        if (entity == null || !entity.Exists()) return -8008;

        return Function.Call<int>(Hash.DECOR_GET_INT, entity, propertyName);
    }

    public static bool DECOR_SET_BOOL(Entity entity, string propertyName, bool value)
    {
        if (entity == null || !entity.Exists()) return false;

        return Function.Call<bool>(Hash.DECOR_SET_BOOL, entity, propertyName, value);
    }

    public static bool _DECOR_SET_FLOAT(Entity entity, string propertyName, float value)
    {
        if (entity == null || !entity.Exists()) return false;

        return Function.Call<bool>(Hash._DECOR_SET_FLOAT, entity, propertyName, value);
    }

    public static bool DECOR_SET_INT(Entity entity, string propertyName, int value)
    {
        if (entity == null || !entity.Exists()) return false;

        return Function.Call<bool>(Hash.DECOR_SET_INT, entity, propertyName, value);
    }

    public static void UnlockDecorators()
    {

        unsafe
        {
            IntPtr addr = (IntPtr)FindPattern("\x40\x53\x48\x83\xEC\x20\x80\x3D\x00\x00\x00\x00\x00\x8B\xDA\x75\x29",
                            "xxxxxxxx????xxxxx");
            if (addr != IntPtr.Zero)
            {
                byte* g_bIsDecorRegisterLockedPtr = (byte*)(addr + *(int*)(addr + 8) + 13);
                *g_bIsDecorRegisterLockedPtr = 0;
            }

        }
    }

    public static void LockDecorators()
    {
        unsafe
        {
            IntPtr addr = (IntPtr)FindPattern("\x40\x53\x48\x83\xEC\x20\x80\x3D\x00\x00\x00\x00\x00\x8B\xDA\x75\x29",
                            "xxxxxxxx????xxxxx");
            if (addr != IntPtr.Zero)
            {
                byte* g_bIsDecorRegisterLockedPtr = (byte*)(addr + *(int*)(addr + 8) + 13);
                *g_bIsDecorRegisterLockedPtr = 1;
            }

        }
    }

    public unsafe static byte* FindPattern(string pattern, string mask)
    {
        ProcessModule module = Process.GetCurrentProcess().MainModule;

        ulong address = (ulong)module.BaseAddress.ToInt64();
        ulong endAddress = address + (ulong)module.ModuleMemorySize;

        for (; address < endAddress; address++)
        {
            for (int i = 0; i < pattern.Length; i++)
            {
                if (mask[i] != '?' && ((byte*)address)[i] != pattern[i])
                {
                    break;
                }
                else if (i + 1 == pattern.Length)
                {
                    return (byte*)address;
                }
            }
        }

        return null;
    }
}
