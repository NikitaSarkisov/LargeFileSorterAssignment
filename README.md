# LargeFileSorter

Проект для выполнения тестового задания.

## Проект

Консольное приложения для сортировки большого текстового файла.

Использовался .NET 5, CommandLineParser.

```powershell
PS D:\publish> .\LargeFileSorter.exe --help
LargeFileSorter 1.0.0
Copyright (C) 2021 LargeFileSorter

  --file          Required. Path to file. If file does not exist random file will be generated.

  --line_count    (Default: 1000) Number of lines for random file generator.

  --max_length    (Default: 50) Max line length for random file generator.

  --validate      (Default: false) Validate whether output file is indeed sorted (for testing).

  --help          Display this help screen.

  --version       Display version information.
```

## Docker:
```
docker run --rm -it -v ${PWD}:/data nikitasarkisov/largefilesorter --file /data/1.txt
```

## Алгоритм

1. Перед началом сортировки приложение составляет "карту строк" (бинарный файл указателей на строки исходного файла).
2. Приложение сортирует карту, считывая строки исходного файла.
3. По отсортированной карте создается выходной файл.

Размер используемой памяти не зависит от входного файла, но зависит от размера максимальной строки.

Алгоритм сортировки использовался: `Comb sort`.

Основная логика находиться в классе `FileSorter` в файле `FileSorter.cs`.

Если входного файла не существует приложение сгенерирует рандомный.

При использовании опции `--validate` приложение проверит готовый файл, что он действительно отсортирован.


## Пример

```
PS D:\publish> .\LargeFileSorter.exe --file 1.txt --line_count 10 --max_length 10 --validate
Generating new random text file...
Number of lines: 10, Max line length: 10
Generated D:\publish\1.txt

Sorting file D:\publish\1.txt...
 - Source file is 73 bytes
 - Building file map...
 - Sorting file map...
 - Writing sorted file...
Done.
Sorted file at: D:\publish\1.txt_sorted.txt
Sorting took 6ms

Validating D:\publish\1.txt_sorted.txt...
Done.
File is valid!
```

```
# Source file: 
PS D:\publish> cat .\1.txt
gbatezt
3oakj1sdi
o
hyamhe
zsw8u4
hy
o09v670
s54i97zf2
rbyma
3
```

```
# Sorted file:
PS D:\publish> cat .\1.txt_sorted.txt
3
3oakj1sdi
gbatezt
hy
hyamhe
o
o09v670
rbyma
```
