import os
import shutil
import subprocess
from os import walk

deploy_path = "C:\Projects\PersikSharp\Builds"
bin_path = ".\\PerchikSharp\\bin\\Debug\\netcoreapp3.1"
config_path = ".\\PerchikSharp\\Configs"
ignore = [".db", ".db-shm", ".db-wal", ".log"]

old_files = []
for (dirpath, dirnames, filenames) in walk(deploy_path):
    old_files.extend(filenames)
    break

for name in old_files:
    if all(ex not in name for ex in ignore):
        full_path = deploy_path + "\\" + name
        if os.path.isfile(full_path):
            os.remove(full_path)
            print("Deleted %s" % name)
        else:
            print("Error: %s file not found" % name)

new_files = []
for (dirpath, dirnames, filenames) in walk(bin_path):
    new_files.extend(filenames)
    break

for name in new_files:
    if all(ex not in name for ex in ignore):
        src_full = bin_path + "\\" + name
        dst_full = deploy_path + "\\" + name
        shutil.copy(src_full, dst_full)
        print("Copied new %s" % name)

config_files = []
for (dirpath, dirnames, filenames) in walk(config_path):
    config_files.extend(filenames)
    break

for name in config_files:
    if all(ex not in name for ex in ignore):
        src_full = config_path + "\\" + name
        dst_full = deploy_path + "\\Configs\\" + name
        shutil.copy(src_full, dst_full)
        print("Copied Config %s" % name)

print("STARTING PERCHIK!")
os.chdir(deploy_path)
os.startfile(deploy_path + "\\PerchikSharp.exe /u")
