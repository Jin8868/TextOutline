基于Unity UGUI的Text描边
实现描边、字体颜色渐变、字体扭曲效果。并且使用公用材质优化合批性能。
![效果预览](img/preview.jpg)

## 修复线性颜色空间下的颜色偏差问题。PlayerSetting如果是Linear，需要在材质上勾选Gamma校正，Gamma则不需要。

### ***Linear未勾选，导致颜色偏差***
![效果预览](img/noGamma.png)

### ***Linear勾选，颜色正常***
![效果预览](img/Gamma.png)

