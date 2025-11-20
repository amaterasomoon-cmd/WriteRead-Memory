// Amateraso Moon
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
// Amateraso Moon

namespace NamoDoApp
{
    internal class GerenciadorMemoriaNativa
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress,
            byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress,
            byte[] lpBuffer, int dwSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr hObject);

        const int PROCESS_ALL_ACCESS = 0x0010 | 0x0020 | 0x0008; 

        private IntPtr handleProcesso;
        private string nomeProcesso;

        public GerenciadorMemoriaNativa(string nomeProcesso)
        {
            this.nomeProcesso = nomeProcesso;
            Inicializar();
        }

        private void Inicializar()
        {
            try
            {
                var processos = Process.GetProcessesByName(nomeProcesso);
                if (processos.Length == 0)
                    throw new Exception($"Processo '{nomeProcesso}' não encontrado");

                var processo = processos[0];
                handleProcesso = OpenProcess(PROCESS_ALL_ACCESS, false, processo.Id);

                if (handleProcesso == IntPtr.Zero)
                    throw new Exception("Falha ao abrir o processo");

                Console.WriteLine($"Processo {nomeProcesso} aberto com sucesso. Handle: {handleProcesso}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro na inicialização: {ex.Message}");
                throw;
            }
        }

        public IntPtr ObterEnderecoBaseModulo(string nomeModulo)
        {
            try
            {
                var processos = Process.GetProcessesByName(nomeProcesso);
                if (processos.Length == 0)
                {
                    Console.WriteLine("Processo não encontrado ao buscar módulo");
                    return IntPtr.Zero;
                }

                var processo = processos[0];

                foreach (ProcessModule modulo in processo.Modules)
                {
                    if (modulo.ModuleName.Equals(nomeModulo, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"Módulo {nomeModulo} encontrado: 0x{modulo.BaseAddress.ToInt64():X}");
                        return modulo.BaseAddress;
                    }
                }

                Console.WriteLine($"Módulo {nomeModulo} não encontrado");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao obter módulo: {ex.Message}");
            }

            return IntPtr.Zero;
        }

        public byte[] LerBytes(IntPtr endereco, int quantidade)
        {
            try
            {
                byte[] buffer = new byte[quantidade];
                bool sucesso = ReadProcessMemory(handleProcesso, endereco, buffer, quantidade, out int bytesLidos);

                if (sucesso && bytesLidos == quantidade)
                {
                    return buffer;
                }
                else
                {
                    Console.WriteLine($"Falha ao ler memória. Sucesso: {sucesso}, Bytes lidos: {bytesLidos}");
                    return new byte[quantidade];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao ler bytes: {ex.Message}");
                return new byte[quantidade];
            }
        }

        public bool EscreverBytes(IntPtr endereco, byte[] bytes)
        {
            try
            {
                bool sucesso = WriteProcessMemory(handleProcesso, endereco, bytes, bytes.Length, out int bytesEscritos);

                if (!sucesso || bytesEscritos != bytes.Length)
                {
                    Console.WriteLine($"Falha ao escrever memória. Sucesso: {sucesso}, Bytes escritos: {bytesEscritos}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao escrever bytes: {ex.Message}");
                return false;
            }
        }

        public bool EscreverNops(IntPtr endereco, int quantidade)
        {
            byte[] nops = new byte[quantidade];
            for (int i = 0; i < quantidade; i++)
                nops[i] = 0x90; 

            return EscreverBytes(endereco, nops);
        }

        public void Fechar()
        {
            if (handleProcesso != IntPtr.Zero)
            {
                CloseHandle(handleProcesso);
                handleProcesso = IntPtr.Zero;
                Console.WriteLine("Handle do processo fechado");
            }
        }

        public void Dispose()
        {
            Fechar();
            GC.SuppressFinalize(this);
        }

        ~GerenciadorMemoriaNativa()
        {
            Fechar();
        }
    }
}

// Amateraso Moon
