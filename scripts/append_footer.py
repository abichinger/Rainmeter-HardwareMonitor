import struct
import sys
import os
from shutil import copyfile

print(sys.argv)

src_path = sys.argv[1]
dst_path = sys.argv[2]

file_size = os.path.getsize(src_path)

key = "RMSKIN\x00"
key = key.encode('utf')

bytes = struct.pack('qB7s', file_size, 0, key)

print(len(bytes), bytes)
hex_str = [hex(b) for b in bytes]
print(hex_str)

copyfile(src_path, dst_path)

with open(dst_path, "ab") as f:
    f.write(bytes)