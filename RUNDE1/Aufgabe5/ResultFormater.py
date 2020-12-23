with open("ausgabe.txt") as f:
  lines = f.readlines()

for i in range(len(lines)):
  lines[i] = lines[i].strip('\n')

with open("fixed.txt", "a", encoding="utf-8") as f:
  f.write(";".join(lines))