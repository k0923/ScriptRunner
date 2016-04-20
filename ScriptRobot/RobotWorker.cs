﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScriptRunner.Interface;
using System.Reflection;
using System.IO;
using Newtonsoft.Json;
using Microsoft.AspNet.SignalR.Client;
using System.Windows;
using ScriptRunner.Interface.Attributes;

namespace ScriptRobot
{
    class RobotWorker
    {


        private ScriptTaskInfo _task;

        public RobotWorker(ScriptTaskInfo taskInfo)
        {
            _task = taskInfo;
        }



        public object Run(Action<StepAttribute> runningStep, Action<StepAttribute> completeStep, Action<ProgressInfo> DetailProgress)
        {
            FileInfo fi = new FileInfo(_task.Location);
            var asmName = AssemblyName.GetAssemblyName(fi.FullName);
            var asm = Assembly.Load(asmName);
            var scriptType = asm.GetType(_task.ScriptType);

            var dataType = scriptType.GetInterfaces().Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IScriptRunner<>)).First().GetGenericArguments().First();


            var jsonMethod = typeof(JsonConvert).GetMethods().Where(m => m.IsGenericMethod && m.Name == nameof(JsonConvert.DeserializeObject) && m.GetParameters()[0].ParameterType == typeof(string)).First();


            var listDataType = typeof(IDictionary<,>).MakeGenericType(typeof(int), dataType);
            var listGenericMethod = jsonMethod.MakeGenericMethod(listDataType);
            dynamic listData = listGenericMethod.Invoke(null, new object[] { _task.JsonData });
            foreach (var item in listData)
            {
                var targetInstance = Activator.CreateInstance(scriptType);

                Type engineType = typeof(ScriptEngine<>).MakeGenericType(new Type[] { dataType });
                var engineInstance = Activator.CreateInstance(engineType, new object[] { targetInstance }) as IScriptEngine;

                engineInstance.StepProgress.ProgressChanged += (s, e) => { DetailProgress(e); };
                engineInstance.BeforeStepExecution += (s) => { runningStep(s); };
                engineInstance.AfterStepExecution += (s) => { completeStep(s); };
                engineInstance.Run(item.Value);
            }


            return listData;

            //var genericMethod = method.MakeGenericMethod(dataType);




            ////_task.JsonData = JsonConvert.SerializeObject(new Test() { Id = 555, UserName = "zholajdls" });

            //object data = genericMethod.Invoke(null, new object[] { _task.JsonData });


            //var targetInstance = Activator.CreateInstance(scriptType);

            //Type engineType = typeof(ScriptEngine<>).MakeGenericType(new Type[] { dataType });
            //var engineInstance = Activator.CreateInstance(engineType, new object[] { targetInstance }) as IScriptEngine;

            //engineInstance.MyProgress.ProgressChanged += (s,e)=> { DetailProgress(e); };
            //engineInstance.BeforeStepExecution += (s) => { runningStep(s); };
            //engineInstance.AfterStepExecution += (s) => { completeStep(s); };
            //engineInstance.Run(data);

            //return data;



        }



    }

}
