using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Native.Csharp.App.LuaEnv
{
    class LuaStates
    {
        //虚拟机池子
        private static ConcurrentDictionary<string, LuaTask.LuaEnv> states =
            new ConcurrentDictionary<string, LuaTask.LuaEnv>();
        //池子操作锁
        private static object stateLock = new object();

        /// <summary>
        /// 添加一个触发事件
        /// 如果虚拟机不存在，则自动新建
        /// </summary>
        /// <param name="name">虚拟机名称</param>
        /// <param name="type">触发类型名</param>
        /// <param name="data">回调数据</param>
        public static void Run(string name, string type, object data)
        {
            lock (stateLock)
            {
                if (!states.ContainsKey(name))//没有的话就初始化池子
                {
                    states[name] = new LuaTask.LuaEnv();
                    states[name].ErrorEvent += (e,text) =>
                    {
                        Common.AppData.CQLog.Error(
                            "Lua插件报错",
                            $"虚拟机运行时错误。名称：{name},错误信息：{text}"
                        );
                    };
                    try
                    {
                        states[name].lua.LoadCLRPackage();
                        states[name].DoFile(Common.AppData.CQApi.AppDirectory + "lua/main.lua");
                    }
                    catch(Exception e)
                    {
                        states[name].Dispose();
                        Common.AppData.CQLog.Error(
                            "Lua插件报错",
                            $"虚拟机启动时错误。名称：{name},错误信息：{e.Message}"
                        );
                        return;
                    }
                }
                states[name].addTigger(type, data);//运行
            }
        }
        public static void Run(long name, string type, object data)
        {
            Run(name.ToString(), type, data);
        }

        /// <summary>
        /// 清空池子
        /// </summary>
        public static void Clear()
        {
            lock (stateLock)
            {
                foreach(string k in states.Keys)
                {
                    LuaTask.LuaEnv l; 
                    states.TryRemove(k, out l);//取出
                    l.Dispose();//释放
                }
            }
        }

        public static string[] GetList()
        {
            lock (stateLock)
            {
                return states.Keys.ToArray();
            }
        }
    }
}
