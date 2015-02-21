XamlCombine
===========
[![Build status](https://img.shields.io/appveyor/ci/batzen/xamlcombine.svg?style=flat-square)](https://ci.appveyor.com/project/batzen/xamlcombine)
[![Release](https://img.shields.io/github/release/fluentribbon/XamlCombine.svg?style=flat-square)](https://github.com/fluentribbon/XamlCombine/releases/latest)
[![Issues](https://img.shields.io/github/issues/fluentribbon/XamlCombine.svg?style=flat-square)](https://github.com/fluentribbon/XamlCombine/issues)

The original code was writting by [SableRaven](https://www.codeplex.com/site/users/view/SableRaven) and was copied from [xamlcombine.codeplex.com](https://xamlcombine.codeplex.com/).

Description
===========
Combines multiple XAML resource dictionaries in one. Replaces DynamicResources to StaticResources. And sort them in order of usage.

Usage
===========
XamlCombine.exe list-of-xamls.txt result-xaml.xaml  

Where:
- list-of-xamls.txt - text file which contains list of XAML filenames, 
- result-xaml.xaml - filename of result XAML file.
